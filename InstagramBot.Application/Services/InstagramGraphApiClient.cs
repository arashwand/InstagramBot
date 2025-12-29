using InstagramBot.Application.Services.Interfaces;
using InstagramBot.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InstagramBot.Application.Services
{
    public class InstagramGraphApiClient : IInstagramGraphApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ICustomLogService _logService;
        private readonly ILogger<InstagramGraphApiClient> _logger;
        private const string BaseUrl = "https://graph.facebook.com/v20.0";

        public InstagramGraphApiClient(
            HttpClient httpClient,
            ICustomLogService logService,
            ILogger<InstagramGraphApiClient> logger)
        {
            _httpClient = httpClient;
            _logService = logService;
            _logger = logger;
        }

        public async Task<List<InstagramMediaDto>> GetMediaAsync(string instagramUserId, string accessToken, int limit = 25)
        {
            try
            {
                var requestUrl = $"{BaseUrl}/{instagramUserId}/media?" +
                               $"fields=id,media_type,media_url,thumbnail_url,caption,permalink,timestamp,like_count,comments_count&" +
                               $"limit={limit}&" +
                               $"access_token={accessToken}";

                var response = await _httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await _logService.LogInstagramApiCallAsync(0, $"GET /media", false, responseContent);
                    throw new InvalidOperationException($"Failed to get media: {responseContent}");
                }

                await _logService.LogInstagramApiCallAsync(0, $"GET /media", true);

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var mediaList = new List<InstagramMediaDto>();

                if (jsonResponse.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        mediaList.Add(ParseMediaFromJson(item));
                    }
                }

                return mediaList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media for Instagram user {InstagramUserId}", instagramUserId);
                throw;
            }
        }

        public async Task<InstagramMediaDto> GetMediaByIdAsync(string mediaId, string accessToken)
        {
            try
            {
                var requestUrl = $"{BaseUrl}/{mediaId}?" +
                               $"fields=id,media_type,media_url,thumbnail_url,caption,permalink,timestamp,like_count,comments_count&" +
                               $"access_token={accessToken}";

                var response = await _httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await _logService.LogInstagramApiCallAsync(0, $"GET /media/{mediaId}", false, responseContent);
                    throw new InvalidOperationException($"Failed to get media by ID: {responseContent}");
                }

                await _logService.LogInstagramApiCallAsync(0, $"GET /media/{mediaId}", true);

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                return ParseMediaFromJson(jsonResponse);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting media by ID {MediaId}", mediaId);
                throw;
            }
        }

        public async Task<List<InstagramCommentDto>> GetMediaCommentsAsync(string mediaId, string accessToken)
        {
            try
            {
                var requestUrl = $"{BaseUrl}/{mediaId}/comments?" +
                               $"fields=id,text,username,timestamp,like_count,replies{{id,text,username,timestamp,like_count}}&" +
                               $"access_token={accessToken}";

                var response = await _httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await _logService.LogInstagramApiCallAsync(0, $"GET /media/{mediaId}/comments", false, responseContent);
                    throw new InvalidOperationException($"Failed to get media comments: {responseContent}");
                }

                await _logService.LogInstagramApiCallAsync(0, $"GET /media/{mediaId}/comments", true);

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var commentsList = new List<InstagramCommentDto>();

                if (jsonResponse.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        commentsList.Add(ParseCommentFromJson(item));
                    }
                }

                return commentsList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting comments for media {MediaId}", mediaId);
                throw;
            }
        }

        public async Task<string> ReplyToCommentAsync(string commentId, string message, string accessToken)
        {
            try
            {
                var requestUrl = $"{BaseUrl}/{commentId}/replies";
                var requestData = new Dictionary<string, string>
                {
                    ["message"] = message,
                    ["access_token"] = accessToken
                };

                var response = await _httpClient.PostAsync(requestUrl, new FormUrlEncodedContent(requestData));
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await _logService.LogInstagramApiCallAsync(0, $"POST /comments/{commentId}/replies", false, responseContent);
                    throw new InvalidOperationException($"Failed to reply to comment: {responseContent}");
                }

                await _logService.LogInstagramApiCallAsync(0, $"POST /comments/{commentId}/replies", true);

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                return jsonResponse.GetProperty("id").GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error replying to comment {CommentId}", commentId);
                throw;
            }
        }

        public async Task<bool> DeleteCommentAsync(string commentId, string accessToken)
        {
            try
            {
                var requestUrl = $"{BaseUrl}/{commentId}?access_token={accessToken}";
                var response = await _httpClient.DeleteAsync(requestUrl);

                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    await _logService.LogInstagramApiCallAsync(0, $"DELETE /comments/{commentId}", false, responseContent);
                    return false;
                }

                await _logService.LogInstagramApiCallAsync(0, $"DELETE /comments/{commentId}", true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting comment {CommentId}", commentId);
                return false;
            }
        }

        public async Task<bool> HideCommentAsync(string commentId, string accessToken)
        {
            try
            {
                var requestUrl = $"{BaseUrl}/{commentId}";
                var requestData = new Dictionary<string, string>
                {
                    ["hide"] = "true",
                    ["access_token"] = accessToken
                };

                var response = await _httpClient.PostAsync(requestUrl, new FormUrlEncodedContent(requestData));

                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    await _logService.LogInstagramApiCallAsync(0, $"POST /comments/{commentId}/hide", false, responseContent);
                    return false;
                }

                await _logService.LogInstagramApiCallAsync(0, $"POST /comments/{commentId}/hide", true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error hiding comment {CommentId}", commentId);
                return false;
            }
        }

        public async Task<string> CreateMediaAsync(string instagramUserId, CreateMediaDto media, string accessToken)
        {
            try
            {
                var requestUrl = $"{BaseUrl}/{instagramUserId}/media";
                var requestData = new Dictionary<string, string>
                {
                    ["access_token"] = accessToken
                };

                if (!string.IsNullOrEmpty(media.ImageUrl))
                {
                    requestData["image_url"] = media.ImageUrl;
                }
                else if (!string.IsNullOrEmpty(media.VideoUrl))
                {
                    requestData["video_url"] = media.VideoUrl;
                    requestData["media_type"] = "VIDEO";
                }

                if (!string.IsNullOrEmpty(media.Caption))
                {
                    requestData["caption"] = media.Caption;
                }

                if (media.UserTags != null && media.UserTags.Any())
                {
                    // User tagging implementation would go here
                    // This requires additional API calls to get user IDs
                }

                if (!string.IsNullOrEmpty(media.LocationId))
                {
                    requestData["location_id"] = media.LocationId;
                }

                var response = await _httpClient.PostAsync(requestUrl, new FormUrlEncodedContent(requestData));
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await _logService.LogInstagramApiCallAsync(0, $"POST /media", false, responseContent);
                    throw new InvalidOperationException($"Failed to create media: {responseContent}");
                }

                await _logService.LogInstagramApiCallAsync(0, $"POST /media", true);

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                return jsonResponse.GetProperty("id").GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating media for Instagram user {InstagramUserId}", instagramUserId);
                throw;
            }
        }

        public async Task<InstagramMediaDto> PublishMediaAsync(string instagramUserId, string creationId, string accessToken)
        {
            try
            {
                var requestUrl = $"{BaseUrl}/{instagramUserId}/media_publish";
                var requestData = new Dictionary<string, string>
                {
                    ["creation_id"] = creationId,
                    ["access_token"] = accessToken
                };

                var response = await _httpClient.PostAsync(requestUrl, new FormUrlEncodedContent(requestData));
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await _logService.LogInstagramApiCallAsync(0, $"POST /media_publish", false, responseContent);
                    throw new InvalidOperationException($"Failed to publish media: {responseContent}");
                }

                await _logService.LogInstagramApiCallAsync(0, $"POST /media_publish", true);

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var mediaId = jsonResponse.GetProperty("id").GetString();

                // دریافت اطلاعات کامل رسانه منتشرشده
                return await GetMediaByIdAsync(mediaId, accessToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing media for Instagram user {InstagramUserId}", instagramUserId);
                throw;
            }
        }

        public async Task<string> CreateStoryAsync(string instagramUserId, InstagramStoryDto story, string accessToken)
        {
            try
            {
                var requestUrl = $"{BaseUrl}/{instagramUserId}/media";
                var requestData = new Dictionary<string, string>
                {
                    ["access_token"] = accessToken
                };

                if (story.MediaType.ToUpper() == "IMAGE")
                {
                    requestData["image_url"] = story.MediaUrl;
                }
                else if (story.MediaType.ToUpper() == "VIDEO")
                {
                    requestData["video_url"] = story.MediaUrl;
                    requestData["media_type"] = "VIDEO";
                }

                if (!string.IsNullOrEmpty(story.Link))
                {
                    requestData["link"] = story.Link;
                }

                // Stories are automatically published, no separate publish step needed
                requestData["media_type"] = "STORIES";

                var response = await _httpClient.PostAsync(requestUrl, new FormUrlEncodedContent(requestData));
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await _logService.LogInstagramApiCallAsync(0, $"POST /stories", false, responseContent);
                    throw new InvalidOperationException($"Failed to create story: {responseContent}");
                }

                await _logService.LogInstagramApiCallAsync(0, $"POST /stories", true);

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                return jsonResponse.GetProperty("id").GetString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating story for Instagram user {InstagramUserId}", instagramUserId);
                throw;
            }
        }

        public async Task<List<InstagramInsightsDto>> GetMediaInsightsAsync(string mediaId, string accessToken)
        {
            try
            {
                var metrics = "impressions,reach,saved,video_views,likes,comments,shares";
                var requestUrl = $"{BaseUrl}/{mediaId}/insights?" +
                               $"metric={metrics}&" +
                               $"access_token={accessToken}";

                var response = await _httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await _logService.LogInstagramApiCallAsync(0, $"GET /media/{mediaId}/insights", false, responseContent);
                    throw new InvalidOperationException($"Failed to get media insights: {responseContent}");
                }

                await _logService.LogInstagramApiCallAsync(0, $"GET /media/{mediaId}/insights", true);

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var insightsList = new List<InstagramInsightsDto>();

                if (jsonResponse.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        insightsList.Add(ParseInsightFromJson(item));
                    }
                }

                return insightsList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting insights for media {MediaId}", mediaId);
                throw;
            }
        }

        public async Task<List<InstagramInsightsDto>> GetAccountInsightsAsync(string instagramUserId, string accessToken, DateTime since, DateTime until)
        {
            try
            {
                var metrics = "impressions,reach,profile_views,follower_count";
                var sinceUnix = ((DateTimeOffset)since).ToUnixTimeSeconds();
                var untilUnix = ((DateTimeOffset)until).ToUnixTimeSeconds();

                var requestUrl = $"{BaseUrl}/{instagramUserId}/insights?" +
                               $"metric={metrics}&" +
                               $"period=day&" +
                               $"since={sinceUnix}&" +
                               $"until={untilUnix}&" +
                               $"access_token={accessToken}";

                var response = await _httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await _logService.LogInstagramApiCallAsync(0, $"GET /insights", false, responseContent);
                    throw new InvalidOperationException($"Failed to get account insights: {responseContent}");
                }

                await _logService.LogInstagramApiCallAsync(0, $"GET /insights", true);

                var jsonResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);
                var insightsList = new List<InstagramInsightsDto>();

                if (jsonResponse.TryGetProperty("data", out var dataArray))
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        insightsList.Add(ParseInsightFromJson(item));
                    }
                }

                return insightsList;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account insights for Instagram user {InstagramUserId}", instagramUserId);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetAccountInfoAsync(string instagramUserId, string accessToken)
        {
            try
            {
                var requestUrl = $"{BaseUrl}/{instagramUserId}?" +
                               $"fields=id,username,name,profile_picture_url,followers_count,follows_count,media_count,biography&" +
                               $"access_token={accessToken}";

                var response = await _httpClient.GetAsync(requestUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    await _logService.LogInstagramApiCallAsync(0, $"GET /account_info", false, responseContent);
                    throw new InvalidOperationException($"Failed to get account info: {responseContent}");
                }

                await _logService.LogInstagramApiCallAsync(0, $"GET /account_info", true);

                var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, object>>(responseContent);
                return jsonResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting account info for Instagram user {InstagramUserId}", instagramUserId);
                throw;
            }
        }

        private InstagramMediaDto ParseMediaFromJson(JsonElement json)
        {
            return new InstagramMediaDto
            {
                Id = json.GetProperty("id").GetString(),
                MediaType = json.TryGetProperty("media_type", out var mediaType) ? mediaType.GetString() : null,
                MediaUrl = json.TryGetProperty("media_url", out var mediaUrl) ? mediaUrl.GetString() : null,
                ThumbnailUrl = json.TryGetProperty("thumbnail_url", out var thumbnailUrl) ? thumbnailUrl.GetString() : null,
                Caption = json.TryGetProperty("caption", out var caption) ? caption.GetString() : null,
                Permalink = json.TryGetProperty("permalink", out var permalink) ? permalink.GetString() : null,
                Timestamp = json.TryGetProperty("timestamp", out var timestamp) ? DateTime.Parse(timestamp.GetString()) : DateTime.MinValue,
                LikeCount = json.TryGetProperty("like_count", out var likeCount) ? likeCount.GetInt32() : 0,
                CommentsCount = json.TryGetProperty("comments_count", out var commentsCount) ? commentsCount.GetInt32() : 0
            };
        }

        private InstagramCommentDto ParseCommentFromJson(JsonElement json)
        {
            var comment = new InstagramCommentDto
            {
                Id = json.GetProperty("id").GetString(),
                Text = json.TryGetProperty("text", out var text) ? text.GetString() : null,
                Username = json.TryGetProperty("username", out var username) ? username.GetString() : null,
                Timestamp = json.TryGetProperty("timestamp", out var timestamp) ? DateTime.Parse(timestamp.GetString()) : DateTime.MinValue,
                LikeCount = json.TryGetProperty("like_count", out var likeCount) ? likeCount.GetInt32() : 0,
                Replies = new List<InstagramCommentDto>()
            };

            if (json.TryGetProperty("replies", out var replies) && replies.TryGetProperty("data", out var repliesData))
            {
                foreach (var reply in repliesData.EnumerateArray())
                {
                    comment.Replies.Add(ParseCommentFromJson(reply));
                }
            }

            return comment;
        }

        private InstagramInsightsDto ParseInsightFromJson(JsonElement json)
        {
            var insight = new InstagramInsightsDto
            {
                Name = json.GetProperty("name").GetString(),
                Period = json.TryGetProperty("period", out var period) ? period.GetString() : null,
                Title = json.TryGetProperty("title", out var title) ? title.GetString() : null,
                Description = json.TryGetProperty("description", out var description) ? description.GetString() : null,
                Values = new List<InstagramInsightValue>()
            };

            if (json.TryGetProperty("values", out var values))
            {
                foreach (var value in values.EnumerateArray())
                {
                    insight.Values.Add(new InstagramInsightValue
                    {
                        Value = value.GetProperty("value").GetInt32(),
                        EndTime = value.TryGetProperty("end_time", out var endTime) ? DateTime.Parse(endTime.GetString()) : null
                    });
                }
            }

            return insight;
        }
    }
}
