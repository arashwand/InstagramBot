using Hangfire;
using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using InstagramBot.DTOs;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InstagramBot.Application.Services
{
    public class AnalyticsCollectionService : IAnalyticsCollectionService
    {
        private readonly IInstagramGraphApiClient _apiClient;
        private readonly IAccountRepository _accountRepository;
        private readonly IPostRepository _postRepository;
        private readonly IAccountAnalyticsRepository _accountAnalyticsRepository;
        private readonly IPostAnalyticsRepository _postAnalyticsRepository;
        private readonly IAnalyticsSnapshotRepository _snapshotRepository;
        private readonly ICustomLogService _logService;
        private readonly ILogger<AnalyticsCollectionService> _logger;

        public AnalyticsCollectionService(
            IInstagramGraphApiClient apiClient,
            IAccountRepository accountRepository,
            IPostRepository postRepository,
            IAccountAnalyticsRepository accountAnalyticsRepository,
            IPostAnalyticsRepository postAnalyticsRepository,
            IAnalyticsSnapshotRepository snapshotRepository,
            ICustomLogService logService,
            ILogger<AnalyticsCollectionService> logger)
        {
            _apiClient = apiClient;
            _accountRepository = accountRepository;
            _postRepository = postRepository;
            _accountAnalyticsRepository = accountAnalyticsRepository;
            _postAnalyticsRepository = postAnalyticsRepository;
            _snapshotRepository = snapshotRepository;
            _logService = logService;
            _logger = logger;
        }

        public async Task CollectAccountAnalyticsAsync(int accountId)
        {
            try
            {
                _logger.LogInformation("Collecting analytics for account {AccountId}", accountId);

                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || !account.IsActive)
                {
                    _logger.LogWarning("Account {AccountId} not found or inactive", accountId);
                    return;
                }

                // دریافت اطلاعات کلی حساب
                var accountInfo = await _apiClient.GetAccountInfoAsync(account.InstagramUserId, account.AccessToken);

                // دریافت آمار حساب برای 7 روز گذشته
                var since = DateTime.UtcNow.AddDays(-7);
                var until = DateTime.UtcNow;
                var insights = await _apiClient.GetAccountInsightsAsync(account.InstagramUserId, account.AccessToken, since, until);

                // ذخیره snapshot خام
                var snapshot = new AnalyticsSnapshot
                {
                    AccountId = accountId,
                    SnapshotDate = DateTime.UtcNow.Date,
                    DataType = "Account",
                    RawData = JsonSerializer.Serialize(new { AccountInfo = accountInfo, Insights = insights }),
                    IsProcessed = false,
                    CreatedDate = DateTime.UtcNow
                };

                await _snapshotRepository.CreateAsync(snapshot);

                // پردازش و ذخیره آمار
                await ProcessAccountInsightsAsync(account, accountInfo, insights);

                await _logService.LogUserActivityAsync(account.UserId, "AnalyticsCollected",
                    $"Analytics collected for account {account.InstagramUsername}");

                _logger.LogInformation("Analytics collected successfully for account {AccountId}", accountId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting analytics for account {AccountId}", accountId);
                await _logService.LogInstagramApiCallAsync(accountId, "CollectAccountAnalytics", false, ex.Message);
            }
        }

        private async Task ProcessAccountInsightsAsync(Account account, Dictionary<string, object> accountInfo, List<InstagramInsightsDto> insights)
        {
            try
            {
                var analytics = new AccountAnalytics
                {
                    AccountId = account.Id,
                    Date = DateTime.UtcNow.Date,
                    CreatedDate = DateTime.UtcNow
                };

                // استخراج اطلاعات از accountInfo
                if (accountInfo.ContainsKey("followers_count"))
                    analytics.FollowersCount = Convert.ToInt32(accountInfo["followers_count"]);

                if (accountInfo.ContainsKey("follows_count"))
                    analytics.FollowingCount = Convert.ToInt32(accountInfo["follows_count"]);

                if (accountInfo.ContainsKey("media_count"))
                    analytics.MediaCount = Convert.ToInt32(accountInfo["media_count"]);

                // استخراج آمار از insights
                foreach (var insight in insights)
                {
                    var latestValue = insight.Values?.LastOrDefault()?.Value ?? 0;

                    switch (insight.Name.ToLower())
                    {
                        case "impressions":
                            analytics.Impressions = latestValue;
                            break;
                        case "reach":
                            analytics.Reach = latestValue;
                            break;
                        case "profile_views":
                            analytics.ProfileViews = latestValue;
                            break;
                        case "website_clicks":
                            analytics.WebsiteClicks = latestValue;
                            break;
                    }
                }

                // محاسبه نرخ تعامل
                if (analytics.FollowersCount > 0 && analytics.Reach > 0)
                {
                    analytics.EngagementRate = (double)analytics.Reach / analytics.FollowersCount * 100;
                }

                // بررسی وجود آمار برای امروز
                var existingAnalytics = await _accountAnalyticsRepository.GetByAccountAndDateAsync(account.Id, DateTime.UtcNow.Date);
                if (existingAnalytics != null)
                {
                    // به‌روزرسانی آمار موجود
                    existingAnalytics.FollowersCount = analytics.FollowersCount;
                    existingAnalytics.FollowingCount = analytics.FollowingCount;
                    existingAnalytics.MediaCount = analytics.MediaCount;
                    existingAnalytics.Impressions = analytics.Impressions;
                    existingAnalytics.Reach = analytics.Reach;
                    existingAnalytics.ProfileViews = analytics.ProfileViews;
                    existingAnalytics.WebsiteClicks = analytics.WebsiteClicks;
                    existingAnalytics.EngagementRate = analytics.EngagementRate;

                    await _accountAnalyticsRepository.UpdateAsync(existingAnalytics);
                }
                else
                {
                    // ایجاد آمار جدید
                    await _accountAnalyticsRepository.CreateAsync(analytics);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing account insights for account {AccountId}", account.Id);
                throw;
            }
        }

        public async Task CollectPostAnalyticsAsync(int postId)
        {
            try
            {
                _logger.LogInformation("Collecting analytics for post {PostId}", postId);

                var post = await _postRepository.GetByIdAsync(postId);
                if (post == null || string.IsNullOrEmpty(post.InstagramMediaId))
                {
                    _logger.LogWarning("Post {PostId} not found or not published", postId);
                    return;
                }

                var account = await _accountRepository.GetByIdAsync(post.AccountId);
                if (account == null || !account.IsActive)
                {
                    _logger.LogWarning("Account for post {PostId} not found or inactive", postId);
                    return;
                }

                // دریافت آمار پست
                var insights = await _apiClient.GetMediaInsightsAsync(post.InstagramMediaId, account.AccessToken);

                // ذخیره snapshot خام
                var snapshot = new AnalyticsSnapshot
                {
                    AccountId = account.Id,
                    SnapshotDate = DateTime.UtcNow.Date,
                    DataType = post.IsStory ? "Story" : "Post",
                    RawData = JsonSerializer.Serialize(insights),
                    IsProcessed = false,
                    CreatedDate = DateTime.UtcNow
                };

                await _snapshotRepository.CreateAsync(snapshot);

                // پردازش و ذخیره آمار
                await ProcessPostInsightsAsync(post, insights);

                _logger.LogInformation("Analytics collected successfully for post {PostId}", postId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error collecting analytics for post {PostId}", postId);
            }
        }

        private async Task ProcessPostInsightsAsync(Post post, List<InstagramInsightsDto> insights)
        {
            try
            {
                var analytics = new PostAnalytics
                {
                    PostId = post.Id,
                    Date = DateTime.UtcNow.Date,
                    CreatedDate = DateTime.UtcNow
                };

                // استخراج آمار از insights
                foreach (var insight in insights)
                {
                    var latestValue = insight.Values?.LastOrDefault()?.Value ?? 0;

                    switch (insight.Name.ToLower())
                    {
                        case "impressions":
                            analytics.Impressions = latestValue;
                            break;
                        case "reach":
                            analytics.Reach = latestValue;
                            break;
                        case "likes":
                            analytics.LikesCount = latestValue;
                            break;
                        case "comments":
                            analytics.CommentsCount = latestValue;
                            break;
                        case "saved":
                            analytics.SavesCount = latestValue;
                            break;
                        case "shares":
                            analytics.SharesCount = latestValue;
                            break;
                        case "video_views":
                            analytics.VideoViews = latestValue;
                            break;
                        case "profile_visits":
                            analytics.ProfileVisits = latestValue;
                            break;
                    }
                }

                // محاسبه نرخ تعامل
                if (analytics.Impressions > 0)
                {
                    var totalEngagements = analytics.LikesCount + analytics.CommentsCount + analytics.SavesCount + analytics.SharesCount;
                    analytics.EngagementRate = (double)totalEngagements / analytics.Impressions * 100;
                }

                // بررسی وجود آمار برای امروز
                var existingAnalytics = await _postAnalyticsRepository.GetByPostAndDateAsync(post.Id, DateTime.UtcNow.Date);
                if (existingAnalytics != null)
                {
                    // به‌روزرسانی آمار موجود
                    existingAnalytics.Impressions = analytics.Impressions;
                    existingAnalytics.Reach = analytics.Reach;
                    existingAnalytics.LikesCount = analytics.LikesCount;
                    existingAnalytics.CommentsCount = analytics.CommentsCount;
                    existingAnalytics.SavesCount = analytics.SavesCount;
                    existingAnalytics.SharesCount = analytics.SharesCount;
                    existingAnalytics.VideoViews = analytics.VideoViews;
                    existingAnalytics.ProfileVisits = analytics.ProfileVisits;
                    existingAnalytics.EngagementRate = analytics.EngagementRate;

                    await _postAnalyticsRepository.UpdateAsync(existingAnalytics);
                }
                else
                {
                    // ایجاد آمار جدید
                    await _postAnalyticsRepository.CreateAsync(analytics);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing post insights for post {PostId}", post.Id);
                throw;
            }
        }

        public async Task CollectAllAccountsAnalyticsAsync()
        {
            try
            {
                _logger.LogInformation("Starting analytics collection for all accounts");

                var activeAccounts = await _accountRepository.GetAllActiveAsync();

                foreach (var account in activeAccounts)
                {
                    try
                    {
                        await CollectAccountAnalyticsAsync(account.Id);

                        // جمع‌آوری آمار پست‌های اخیر
                        var recentPosts = await _postRepository.GetRecentPublishedPostsAsync(account.Id, 30); // 30 روز گذشته

                        foreach (var post in recentPosts)
                        {
                            await CollectPostAnalyticsAsync(post.Id);
                            await Task.Delay(2000); // تاخیر برای جلوگیری از rate limiting
                        }

                        await Task.Delay(5000); // تاخیر بین حساب‌ها
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error collecting analytics for account {AccountId}", account.Id);
                    }
                }

                _logger.LogInformation("Analytics collection completed for all accounts");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk analytics collection");
            }
        }

        public async Task ProcessPendingAnalyticsAsync()
        {
            try
            {
                var pendingSnapshots = await _snapshotRepository.GetPendingSnapshotsAsync();

                foreach (var snapshot in pendingSnapshots)
                {
                    try
                    {
                        // پردازش بر اساس نوع داده
                        if (snapshot.DataType == "Account")
                        {
                            // پردازش آمار حساب
                            // کد پردازش...
                        }
                        else if (snapshot.DataType == "Post" || snapshot.DataType == "Story")
                        {
                            // پردازش آمار پست
                            // کد پردازش...
                        }

                        snapshot.IsProcessed = true;
                        await _snapshotRepository.UpdateAsync(snapshot);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing snapshot {SnapshotId}", snapshot.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing pending analytics");
            }
        }

        public async Task<AccountAnalyticsDto> GetLatestAccountAnalyticsAsync(int accountId)
        {
            var analytics = await _accountAnalyticsRepository.GetLatestByAccountIdAsync(accountId);
            if (analytics == null) return null;

            return new AccountAnalyticsDto
            {
                AccountId = analytics.AccountId,
                AccountUsername = analytics.Account?.InstagramUsername,
                Date = analytics.Date,
                FollowersCount = analytics.FollowersCount,
                FollowingCount = analytics.FollowingCount,
                MediaCount = analytics.MediaCount,
                Impressions = analytics.Impressions,
                Reach = analytics.Reach,
                ProfileViews = analytics.ProfileViews,
                WebsiteClicks = analytics.WebsiteClicks,
                EngagementRate = analytics.EngagementRate
            };
        }

        public async Task<List<PostAnalyticsDto>> GetPostAnalyticsAsync(int accountId, DateTime fromDate, DateTime toDate)
        {
            var analyticsData = await _postAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate);

            return analyticsData.Select(a => new PostAnalyticsDto
            {
                PostId = a.PostId,
                InstagramMediaId = a.Post?.InstagramMediaId,
                Date = a.Date,
                Impressions = a.Impressions,
                Reach = a.Reach,
                LikesCount = a.LikesCount,
                CommentsCount = a.CommentsCount,
                SavesCount = a.SavesCount,
                SharesCount = a.SharesCount,
                VideoViews = a.VideoViews,
                ProfileVisits = a.ProfileVisits,
                EngagementRate = a.EngagementRate,
                AudienceGender = ParseJsonToDictionary(a.AudienceGender),
                AudienceAge = ParseJsonToDictionary(a.AudienceAge),
                AudienceCountry = ParseJsonToDictionary(a.AudienceCountry)
            }).ToList();
        }

        public async Task ScheduleAnalyticsCollectionAsync()
        {
            // زمان‌بندی جمع‌آوری روزانه آمار در ساعت 1 صبح
            RecurringJob.AddOrUpdate<IAnalyticsCollectionService>(
                "collect-all-analytics",
                service => service.CollectAllAccountsAnalyticsAsync(),
                "0 1 * * *", // هر روز ساعت 1 صبح
                TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"));

            // زمان‌بندی پردازش snapshot های معلق
            RecurringJob.AddOrUpdate<IAnalyticsCollectionService>(
                "process-pending-analytics",
                service => service.ProcessPendingAnalyticsAsync(),
                "*/30 * * * *", // هر 30 دقیقه
                TimeZoneInfo.FindSystemTimeZoneById("Iran Standard Time"));

            _logger.LogInformation("Analytics collection jobs scheduled successfully");
        }

        private Dictionary<string, int> ParseJsonToDictionary(string json)
        {
            if (string.IsNullOrEmpty(json))
                return new Dictionary<string, int>();

            try
            {
                return JsonSerializer.Deserialize<Dictionary<string, int>>(json) ?? new Dictionary<string, int>();
            }
            catch
            {
                return new Dictionary<string, int>();
            }
        }
    }
}
