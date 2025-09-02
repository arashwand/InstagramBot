using InstagramBot.Application.DTOs;
using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InstagramBot.Application.Services
{
    public class InstagramOAuthService : IInstagramOAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IAccountRepository _accountRepository;
        private readonly IEncryptionService _encryptionService;
        private readonly ICustomLogService _logService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<InstagramOAuthService> _logger;

        private readonly string _appId;
        private readonly string _appSecret;
        private readonly string _redirectUri;
        private readonly string _scopes;

        public InstagramOAuthService(
            HttpClient httpClient,
            IConfiguration configuration,
            IAccountRepository accountRepository,
            IEncryptionService encryptionService,
            ICustomLogService logService,
            IMemoryCache cache,
            ILogger<InstagramOAuthService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _accountRepository = accountRepository;
            _encryptionService = encryptionService;
            _logService = logService;
            _cache = cache;
            _logger = logger;

            _appId = _configuration["Instagram:AppId"];
            _appSecret = _configuration["Instagram:AppSecret"];
            _redirectUri = _configuration["Instagram:RedirectUri"];
            _scopes = _configuration["Instagram:Scopes"];
        }

        public string GenerateAuthorizationUrl(int userId)
        {
            var state = GenerateState(userId);
            _cache.Set($"oauth_state_{state}", userId, TimeSpan.FromMinutes(10));

            var authUrl = $"https://www.facebook.com/v20.0/dialog/oauth?" +
                         $"client_id={_appId}&" +
                         $"redirect_uri={Uri.EscapeDataString(_redirectUri)}&" +
                         $"scope={Uri.EscapeDataString(_scopes)}&" +
                         $"response_type=code&" +
                         $"state={state}";

            _logger.LogInformation("Generated authorization URL for user {UserId}", userId);
            return authUrl;
        }

        public async Task<Account> HandleCallbackAsync(int userId, InstagramCallbackDto callback)
        {
            try
            {
                // اعتبارسنجی State
                if (!_cache.TryGetValue($"oauth_state_{callback.State}", out int cachedUserId) ||
                    cachedUserId != userId)
                {
                    throw new UnauthorizedAccessException("State parameter is invalid");
                }

                _cache.Remove($"oauth_state_{callback.State}");

                if (!string.IsNullOrEmpty(callback.Error))
                {
                    await _logService.LogSecurityEventAsync(userId, "OAuthError",
                        $"OAuth error: {callback.Error} - {callback.ErrorDescription}");
                    throw new InvalidOperationException($"OAuth Error: {callback.ErrorDescription}");
                }

                // دریافت Access Token
                var accessToken = await ExchangeCodeForTokenAsync(callback.Code);

                // تبدیل به Long-Lived Token
                var longLivedToken = await GetLongLivedTokenAsync(accessToken);

                // دریافت صفحات فیسبوک
                var pages = await GetFacebookPagesAsync(longLivedToken.AccessToken);

                // پیدا کردن صفحه با حساب اینستاگرام
                var pageWithInstagram = await FindPageWithInstagramAsync(pages, longLivedToken.AccessToken);

                if (pageWithInstagram == null)
                {
                    throw new InvalidOperationException("هیچ حساب اینستاگرام بیزنس متصل به صفحات فیسبوک شما یافت نشد.");
                }

                // دریافت اطلاعات حساب اینستاگرام
                var instagramAccount = await GetInstagramBusinessAccountAsync(pageWithInstagram.Id, pageWithInstagram.AccessToken);

                // ایجاد رکورد Account
                var account = new Account
                {
                    UserId = userId,
                    InstagramUserId = instagramAccount.Id,
                    InstagramUsername = instagramAccount.Username,
                    AccessToken = longLivedToken.AccessToken,
                    ExpiresIn = longLivedToken.ExpiresIn,
                    LastRefreshed = DateTime.UtcNow,
                    PageAccessToken = pageWithInstagram.AccessToken,
                    PageId = pageWithInstagram.Id,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow
                };

                var savedAccount = await _accountRepository.CreateAsync(account);

                await _logService.LogUserActivityAsync(userId, "InstagramAccountConnected",
                    $"Connected Instagram account: {instagramAccount.Username}");

                _logger.LogInformation("Successfully connected Instagram account {Username} for user {UserId}",
                    instagramAccount.Username, userId);

                return savedAccount;
            }
            catch (Exception ex)
            {
                await _logService.LogSecurityEventAsync(userId, "OAuthCallbackError", ex.Message);
                _logger.LogError(ex, "Error handling OAuth callback for user {UserId}", userId);
                throw;
            }
        }

        private async Task<string> ExchangeCodeForTokenAsync(string code)
        {
            var requestUrl = "https://graph.facebook.com/v20.0/oauth/access_token";
            var requestData = new Dictionary<string, string>
            {
                ["client_id"] = _appId,
                ["client_secret"] = _appSecret,
                ["redirect_uri"] = _redirectUri,
                ["code"] = code
            };

            var response = await _httpClient.PostAsync(requestUrl, new FormUrlEncodedContent(requestData));
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to exchange code for token: {Response}", responseContent);
                throw new InvalidOperationException("Failed to exchange authorization code for access token");
            }

            var tokenResponse = JsonSerializer.Deserialize<InstagramTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return tokenResponse.AccessToken;
        }

        private async Task<InstagramLongLivedTokenResponse> GetLongLivedTokenAsync(string shortLivedToken)
        {
            var requestUrl = $"https://graph.facebook.com/v20.0/oauth/access_token?" +
                           $"grant_type=fb_exchange_token&" +
                           $"client_id={_appId}&" +
                           $"client_secret={_appSecret}&" +
                           $"fb_exchange_token={shortLivedToken}";

            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get long-lived token: {Response}", responseContent);
                throw new InvalidOperationException("Failed to get long-lived access token");
            }

            var tokenResponse = JsonSerializer.Deserialize<InstagramLongLivedTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return tokenResponse;
        }

        private async Task<List<FacebookPage>> GetFacebookPagesAsync(string accessToken)
        {
            var requestUrl = $"https://graph.facebook.com/v20.0/me/accounts?access_token={accessToken}";

            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get Facebook pages: {Response}", responseContent);
                throw new InvalidOperationException("Failed to retrieve Facebook pages");
            }

            var pagesResponse = JsonSerializer.Deserialize<FacebookPageResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return pagesResponse.Data;
        }

        private async Task<FacebookPage> FindPageWithInstagramAsync(List<FacebookPage> pages, string userAccessToken)
        {
            foreach (var page in pages)
            {
                try
                {
                    var requestUrl = $"https://graph.facebook.com/v20.0/{page.Id}?" +
                                   $"fields=instagram_business_account&" +
                                   $"access_token={page.AccessToken}";

                    var response = await _httpClient.GetAsync(requestUrl);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        var pageData = JsonSerializer.Deserialize<JsonElement>(responseContent);
                        if (pageData.TryGetProperty("instagram_business_account", out var instagramAccount))
                        {
                            return page;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error checking Instagram account for page {PageId}", page.Id);
                }
            }

            return null;
        }

        private async Task<InstagramBusinessAccount> GetInstagramBusinessAccountAsync(string pageId, string pageAccessToken)
        {
            var requestUrl = $"https://graph.facebook.com/v20.0/{pageId}?" +
                           $"fields=instagram_business_account{{id,username,name,profile_picture_url,followers_count,follows_count,media_count}}&" +
                           $"access_token={pageAccessToken}";

            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to get Instagram business account: {Response}", responseContent);
                throw new InvalidOperationException("Failed to retrieve Instagram business account information");
            }

            var pageData = JsonSerializer.Deserialize<JsonElement>(responseContent);
            var instagramData = pageData.GetProperty("instagram_business_account");

            return new InstagramBusinessAccount
            {
                Id = instagramData.GetProperty("id").GetString(),
                Username = instagramData.GetProperty("username").GetString(),
                Name = instagramData.TryGetProperty("name", out var name) ? name.GetString() : null,
                ProfilePictureUrl = instagramData.TryGetProperty("profile_picture_url", out var pic) ? pic.GetString() : null,
                FollowersCount = instagramData.TryGetProperty("followers_count", out var followers) ? followers.GetInt32() : 0,
                FollowsCount = instagramData.TryGetProperty("follows_count", out var follows) ? follows.GetInt32() : 0,
                MediaCount = instagramData.TryGetProperty("media_count", out var media) ? media.GetInt32() : 0
            };
        }

        public async Task<string> RefreshTokenAsync(string accessToken)
        {
            var requestUrl = $"https://graph.facebook.com/v20.0/oauth/access_token?" +
                           $"grant_type=fb_exchange_token&" +
                           $"client_id={_appId}&" +
                           $"client_secret={_appSecret}&" +
                           $"fb_exchange_token={accessToken}";

            var response = await _httpClient.GetAsync(requestUrl);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to refresh token: {Response}", responseContent);
                throw new InvalidOperationException("Failed to refresh access token");
            }

            var tokenResponse = JsonSerializer.Deserialize<InstagramLongLivedTokenResponse>(responseContent, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            });

            return tokenResponse.AccessToken;
        }

        public async Task<bool> ValidateTokenAsync(string accessToken)
        {
            try
            {
                var requestUrl = $"https://graph.facebook.com/v20.0/me?access_token={accessToken}";
                var response = await _httpClient.GetAsync(requestUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        private string GenerateState(int userId)
        {
            var data = $"{userId}:{DateTime.UtcNow.Ticks}:{Guid.NewGuid()}";
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hash).Replace("+", "-").Replace("/", "_").Replace("=", "");
        }

    }
}

