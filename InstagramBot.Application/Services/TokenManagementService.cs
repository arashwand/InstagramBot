using Hangfire;
using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace InstagramBot.Application.Services
{
    public class TokenManagementService : ITokenManagementService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IInstagramOAuthService _oauthService;
        private readonly ICustomLogService _logService;
        private readonly ILogger<TokenManagementService> _logger;

        public TokenManagementService(
            IAccountRepository accountRepository,
            IInstagramOAuthService oauthService,
            ICustomLogService logService,
            ILogger<TokenManagementService> logger)
        {
            _accountRepository = accountRepository;
            _oauthService = oauthService;
            _logService = logService;
            _logger = logger;
        }

        public async Task RefreshExpiredTokensAsync()
        {
            try
            {
                _logger.LogInformation("Starting token refresh process");

                // دریافت حساب‌هایی که توکن‌شان در 7 روز آینده منقضی می‌شود
                var expiringAccounts = await GetExpiringAccountsAsync();

                foreach (var account in expiringAccounts)
                {
                    try
                    {
                        await RefreshTokenForAccountAsync(account.Id);
                        await Task.Delay(1000); // تاخیر برای جلوگیری از rate limiting
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to refresh token for account {AccountId}", account.Id);
                        await _logService.LogSecurityEventAsync(account.UserId, "TokenRefreshFailed",
                            $"Failed to refresh token for Instagram account {account.InstagramUsername}: {ex.Message}");
                    }
                }

                _logger.LogInformation("Token refresh process completed. Processed {Count} accounts", expiringAccounts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in token refresh process");
            }
        }

        public async Task RefreshTokenForAccountAsync(int accountId)
        {
            var account = await _accountRepository.GetByIdAsync(accountId);
            if (account == null)
            {
                throw new ArgumentException($"Account with ID {accountId} not found");
            }

            try
            {
                _logger.LogInformation("Refreshing token for account {AccountId} ({Username})",
                    account.Id, account.InstagramUsername);

                // بررسی اعتبار توکن فعلی
                var isValid = await _oauthService.ValidateTokenAsync(account.AccessToken);
                if (!isValid)
                {
                    _logger.LogWarning("Current token is invalid for account {AccountId}", account.Id);
                    account.IsActive = false;
                    await _accountRepository.UpdateAsync(account);
                    return;
                }

                // رفرش توکن
                var newToken = await _oauthService.RefreshTokenAsync(account.AccessToken);

                // به‌روزرسانی اطلاعات حساب
                account.AccessToken = newToken;
                account.LastRefreshed = DateTime.UtcNow;
                account.ExpiresIn = 5184000; // 60 days in seconds (default for long-lived tokens)

                await _accountRepository.UpdateAsync(account);

                await _logService.LogUserActivityAsync(account.UserId, "TokenRefreshed",
                    $"Token refreshed for Instagram account {account.InstagramUsername}");

                _logger.LogInformation("Successfully refreshed token for account {AccountId}", account.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh token for account {AccountId}", account.Id);

                // در صورت خطا، حساب را غیرفعال کن
                account.IsActive = false;
                await _accountRepository.UpdateAsync(account);

                throw;
            }
        }

        public async Task ValidateAllTokensAsync()
        {
            try
            {
                _logger.LogInformation("Starting token validation process");

                var allAccounts = await GetAllActiveAccountsAsync();

                foreach (var account in allAccounts)
                {
                    try
                    {
                        var isValid = await _oauthService.ValidateTokenAsync(account.AccessToken);
                        if (!isValid)
                        {
                            _logger.LogWarning("Invalid token detected for account {AccountId} ({Username})",
                                account.Id, account.InstagramUsername);

                            account.IsActive = false;
                            await _accountRepository.UpdateAsync(account);

                            await _logService.LogSecurityEventAsync(account.UserId, "InvalidTokenDetected",
                                $"Invalid token detected for Instagram account {account.InstagramUsername}");
                        }

                        await Task.Delay(500); // تاخیر برای جلوگیری از rate limiting
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error validating token for account {AccountId}", account.Id);
                    }
                }

                _logger.LogInformation("Token validation process completed. Validated {Count} accounts", allAccounts.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in token validation process");
            }
        }

        public async Task ScheduleTokenRefreshAsync()
        {
            RecurringJobOptions recurringJobOptions = new RecurringJobOptions();
            recurringJobOptions.TimeZone = TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time");

            // زمان‌بندی رفرش روزانه توکن‌ها در ساعت 2 صبح
            RecurringJob.AddOrUpdate<ITokenManagementService>(
                "refresh-expired-tokens",
                service => service.RefreshExpiredTokensAsync(),
                "0 2 * * *", // هر روز ساعت 2 صبح
                recurringJobOptions);

            // زمان‌بندی اعتبارسنجی هفتگی توکن‌ها
            RecurringJob.AddOrUpdate<ITokenManagementService>(
                "validate-all-tokens",
                service => service.ValidateAllTokensAsync(),
                "0 3 * * 0", // هر یکشنبه ساعت 3 صبح
                recurringJobOptions);

            _logger.LogInformation("Token management jobs scheduled successfully");
        }

        private async Task<List<Account>> GetExpiringAccountsAsync()
        {
            // این متد باید در Repository پیاده‌سازی شود
            // برای سادگی، اینجا یک پیاده‌سازی ساده ارائه می‌دهیم
            var allAccounts = await GetAllActiveAccountsAsync();
            var expiringAccounts = new List<Account>();

            foreach (var account in allAccounts)
            {
                var expiryDate = account.LastRefreshed.AddSeconds(account.ExpiresIn);
                var daysUntilExpiry = (expiryDate - DateTime.UtcNow).TotalDays;

                if (daysUntilExpiry <= 7) // توکن‌هایی که در 7 روز آینده منقضی می‌شوند
                {
                    expiringAccounts.Add(account);
                }
            }

            return expiringAccounts;
        }

        private async Task<List<Account>> GetAllActiveAccountsAsync()
        {
            // این متد باید در Repository پیاده‌سازی شود
            // برای سادگی، فرض می‌کنیم که متد GetAllActiveAsync وجود دارد
            // در عمل، باید یک کوئری مناسب در Repository نوشته شود
            throw new NotImplementedException("This method should be implemented in AccountRepository");
        }
    }
}

