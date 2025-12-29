using InstagramBot.Application.Services;
using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Interfaces;
using InstagramBot.DTOs;
using InstagramBot.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace InstagramBot.Infrastructure.Repositories
{
    public class ReportingService : IReportingService
    {
        private readonly IAccountAnalyticsRepository _accountAnalyticsRepository;
        private readonly IPostAnalyticsRepository _postAnalyticsRepository;
        private readonly IPostRepository _postRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IInstagramGraphApiClient _apiClient;
        private readonly ILogger<ReportingService> _logger;

        public ReportingService(
            IAccountAnalyticsRepository accountAnalyticsRepository,
            IPostAnalyticsRepository postAnalyticsRepository,
            IPostRepository postRepository,
            IAccountRepository accountRepository,
            IInstagramGraphApiClient apiClient,
            ILogger<ReportingService> logger)
        {
            _accountAnalyticsRepository = accountAnalyticsRepository;
            _postAnalyticsRepository = postAnalyticsRepository;
            _postRepository = postRepository;
            _accountRepository = accountRepository;
            _apiClient = apiClient;
            _logger = logger;
        }

        public async Task<AnalyticsReportDto> GenerateAccountReportAsync(int accountId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                _logger.LogInformation("Generating report for account {AccountId} from {FromDate} to {ToDate}",
                    accountId, fromDate, toDate);

                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null)
                    throw new ArgumentException("حساب یافت نشد.");

                // دریافت آمار حساب
                var accountAnalytics = await _accountAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate);
                var postAnalytics = await _postAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate);

                // محاسبه آمار کلی
                var totalImpressions = postAnalytics.Sum(p => p.Impressions);
                var totalReach = postAnalytics.Sum(p => p.Reach);
                var totalLikes = postAnalytics.Sum(p => p.LikesCount);
                var totalComments = postAnalytics.Sum(p => p.CommentsCount);
                var averageEngagement = postAnalytics.Any() ? postAnalytics.Average(p => p.EngagementRate) : 0;

                // محاسبه رشد فالوور
                var firstDayAnalytics = accountAnalytics.OrderBy(a => a.Date).FirstOrDefault();
                var lastDayAnalytics = accountAnalytics.OrderByDescending(a => a.Date).FirstOrDefault();
                var followersGrowth = lastDayAnalytics != null && firstDayAnalytics != null
                    ? lastDayAnalytics.FollowersCount - firstDayAnalytics.FollowersCount
                    : 0;

                // دریافت بهترین پست‌ها
                var topPosts = await GetTopPerformingPostsAsync(accountId, fromDate, toDate, 5);

                // آمار پست‌ها بر اساس روز
                var postsByDay = await GetPostsByDayAsync(accountId, fromDate, toDate);

                // آمار تعامل بر اساس ساعت
                var engagementByHour = await GetEngagementByHourAsync(accountId, fromDate, toDate);

                var report = new AnalyticsReportDto
                {
                    FromDate = fromDate,
                    ToDate = toDate,
                    TotalPosts = postAnalytics.Count,
                    TotalStories = postAnalytics.Count(p => p.Post?.IsStory == true),
                    TotalImpressions = totalImpressions,
                    TotalReach = totalReach,
                    TotalLikes = totalLikes,
                    TotalComments = totalComments,
                    AverageEngagementRate = averageEngagement,
                    FollowersGrowth = followersGrowth,
                    TopPosts = topPosts,
                    PostsByDay = postsByDay,
                    EngagementByHour = engagementByHour
                };

                _logger.LogInformation("Report generated successfully for account {AccountId}", accountId);
                return report;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report for account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<List<PostAnalyticsDto>> GetTopPerformingPostsAsync(int accountId, DateTime fromDate, DateTime toDate, int count = 10)
        {
            var topPosts = await _postAnalyticsRepository.GetTopPostsByEngagementAsync(accountId, fromDate, toDate, count);

            return topPosts.Select(p => new PostAnalyticsDto
            {
                PostId = p.PostId,
                InstagramMediaId = p.Post?.InstagramMediaId,
                Date = p.Date,
                Impressions = p.Impressions,
                Reach = p.Reach,
                LikesCount = p.LikesCount,
                CommentsCount = p.CommentsCount,
                SavesCount = p.SavesCount,
                SharesCount = p.SharesCount,
                VideoViews = p.VideoViews,
                ProfileVisits = p.ProfileVisits,
                EngagementRate = p.EngagementRate
            }).ToList();
        }

        public async Task<Dictionary<string, object>> GetEngagementTrendsAsync(int accountId, DateTime fromDate, DateTime toDate)
        {
            var postAnalytics = await _postAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate);

            var trends = postAnalytics
                .GroupBy(p => p.Date.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key.ToString("yyyy-MM-dd"),
                    g => (object)new
                    {
                        Date = g.Key,
                        AverageEngagement = g.Average(p => p.EngagementRate),
                        TotalLikes = g.Sum(p => p.LikesCount),
                        TotalComments = g.Sum(p => p.CommentsCount),
                        TotalImpressions = g.Sum(p => p.Impressions),
                        PostCount = g.Count()
                    }
                );

            return new Dictionary<string, object>
            {
                ["trends"] = trends,
                ["summary"] = new
                {
                    AverageEngagement = postAnalytics.Any() ? postAnalytics.Average(p => p.EngagementRate) : 0,
                    BestDay = trends.OrderByDescending(t => ((dynamic)t.Value).AverageEngagement).FirstOrDefault().Key,
                    TotalEngagements = postAnalytics.Sum(p => p.LikesCount + p.CommentsCount + p.SavesCount)
                }
            };
        }

        public async Task<Dictionary<string, object>> GetAudienceInsightsAsync(int accountId, DateTime fromDate, DateTime toDate)
        {
            var postAnalytics = await _postAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate);

            var genderData = new Dictionary<string, int>();
            var ageData = new Dictionary<string, int>();
            var countryData = new Dictionary<string, int>();

            foreach (var post in postAnalytics)
            {
                // ترکیب داده‌های جمعیت‌شناختی
                if (!string.IsNullOrEmpty(post.AudienceGender))
                {
                    var gender = JsonSerializer.Deserialize<Dictionary<string, int>>(post.AudienceGender);
                    foreach (var kvp in gender)
                    {
                        genderData[kvp.Key] = genderData.GetValueOrDefault(kvp.Key, 0) + kvp.Value;
                    }
                }

                if (!string.IsNullOrEmpty(post.AudienceAge))
                {
                    var age = JsonSerializer.Deserialize<Dictionary<string, int>>(post.AudienceAge);
                    foreach (var kvp in age)
                    {
                        ageData[kvp.Key] = ageData.GetValueOrDefault(kvp.Key, 0) + kvp.Value;
                    }
                }

                if (!string.IsNullOrEmpty(post.AudienceCountry))
                {
                    var country = JsonSerializer.Deserialize<Dictionary<string, int>>(post.AudienceCountry);
                    foreach (var kvp in country)
                    {
                        countryData[kvp.Key] = countryData.GetValueOrDefault(kvp.Key, 0) + kvp.Value;
                    }
                }
            }

            return new Dictionary<string, object>
            {
                ["gender"] = genderData.OrderByDescending(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ["age"] = ageData.OrderByDescending(kvp => kvp.Value).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ["country"] = countryData.OrderByDescending(kvp => kvp.Value).Take(10).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
            };
        }

        public async Task<Dictionary<string, object>> GetBestPostingTimesAsync(int accountId)
        {
            var posts = await _postRepository.GetPublishedPostsAsync(accountId);
            var postAnalytics = await _postAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, DateTime.UtcNow.AddDays(-90), DateTime.UtcNow);

            var postPerformance = posts
                .Where(p => p.PublishedDate.HasValue)
                .Join(postAnalytics, p => p.Id, a => a.PostId, (p, a) => new { Post = p, Analytics = a })
                .ToList();

            // تحلیل بر اساس ساعت
            var hourlyPerformance = postPerformance
                .GroupBy(pp => pp.Post.PublishedDate.Value.Hour)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Average(pp => pp.Analytics.EngagementRate)
                );

            // تحلیل بر اساس روز هفته
            var dailyPerformance = postPerformance
                .GroupBy(pp => pp.Post.PublishedDate.Value.DayOfWeek)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Average(pp => pp.Analytics.EngagementRate)
                );

            var bestHour = hourlyPerformance.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
            var bestDay = dailyPerformance.OrderByDescending(kvp => kvp.Value).FirstOrDefault();

            return new Dictionary<string, object>
            {
                ["hourlyPerformance"] = hourlyPerformance,
                ["dailyPerformance"] = dailyPerformance,
                ["recommendations"] = new
                {
                    BestHour = bestHour.Key,
                    BestDay = bestDay.Key,
                    OptimalTimes = hourlyPerformance
                        .OrderByDescending(kvp => kvp.Value)
                        .Take(3)
                        .Select(kvp => $"{kvp.Key}:00")
                        .ToList()
                }
            };
        }

        public async Task<Dictionary<string, object>> GetHashtagPerformanceAsync(int accountId, DateTime fromDate, DateTime toDate)
        {
            var posts = await _postRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate);
            var postAnalytics = await _postAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate);

            var hashtagPerformance = new Dictionary<string, List<double>>();

            foreach (var post in posts)
            {
                if (string.IsNullOrEmpty(post.Caption)) continue;

                var hashtags = ExtractHashtags(post.Caption);
                var analytics = postAnalytics.FirstOrDefault(a => a.PostId == post.Id);

                if (analytics != null)
                {
                    foreach (var hashtag in hashtags)
                    {
                        if (!hashtagPerformance.ContainsKey(hashtag))
                            hashtagPerformance[hashtag] = new List<double>();

                        hashtagPerformance[hashtag].Add(analytics.EngagementRate);
                    }
                }
            }

            var hashtagStats = hashtagPerformance
                .Where(kvp => kvp.Value.Count >= 2) // حداقل 2 بار استفاده شده
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => new
                    {
                        UsageCount = kvp.Value.Count,
                        AverageEngagement = kvp.Value.Average(),
                        MaxEngagement = kvp.Value.Max(),
                        MinEngagement = kvp.Value.Min()
                    }
                )
                .OrderByDescending(kvp => kvp.Value.AverageEngagement)
                .Take(20)
                .ToDictionary(kvp => kvp.Key, kvp => (object)kvp.Value);

            return new Dictionary<string, object>
            {
                ["hashtagStats"] = hashtagStats,
                ["topHashtags"] = hashtagStats.Take(10).ToDictionary(kvp => kvp.Key, kvp => kvp.Value),
                ["recommendations"] = hashtagStats
                    .Where(kvp => ((dynamic)kvp.Value).UsageCount >= 3)
                    .Take(5)
                    .Select(kvp => kvp.Key)
                    .ToList()
            };
        }

        public async Task<Dictionary<string, object>> GetCompetitorAnalysisAsync(int accountId, List<string> competitorUsernames)
        {
            // این بخش نیاز به دسترسی به اطلاعات عمومی رقبا دارد
            // که ممکن است محدودیت‌هایی داشته باشد

            var account = await _accountRepository.GetByIdAsync(accountId);
            var myAnalytics = await _accountAnalyticsRepository.GetLatestByAccountIdAsync(accountId);

            var competitorData = new List<object>();

            foreach (var username in competitorUsernames)
            {
                try
                {
                    // دریافت اطلاعات عمومی رقیب (اگر امکان‌پذیر باشد)
                    // var competitorInfo = await _apiClient.GetPublicAccountInfoAsync(username);

                    competitorData.Add(new
                    {
                        Username = username,
                        // FollowersCount = competitorInfo.FollowersCount,
                        // MediaCount = competitorInfo.MediaCount,
                        // AverageEngagement = CalculateEstimatedEngagement(competitorInfo)
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not fetch competitor data for {Username}", username);
                }
            }

            return new Dictionary<string, object>
            {
                ["myAccount"] = new
                {
                    Username = account.InstagramUsername,
                    FollowersCount = myAnalytics?.FollowersCount ?? 0,
                    EngagementRate = myAnalytics?.EngagementRate ?? 0
                },
                ["competitors"] = competitorData,
                ["analysis"] = new
                {
                    MyRanking = "محاسبه بر اساس داده‌های موجود",
                    Recommendations = new[]
                    {
                        "بهبود کیفیت محتوا",
                        "افزایش تعامل با مخاطبان",
                        "استفاده از هشتگ‌های مؤثر"
                    }
                }
            };
        }

        private async Task<Dictionary<string, int>> GetPostsByDayAsync(int accountId, DateTime fromDate, DateTime toDate)
        {
            var posts = await _postRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate);

            return posts
                .Where(p => p.PublishedDate.HasValue)
                .GroupBy(p => p.PublishedDate.Value.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key.ToString("yyyy-MM-dd"),
                    g => g.Count()
                );
        }

        private async Task<Dictionary<string, double>> GetEngagementByHourAsync(int accountId, DateTime fromDate, DateTime toDate)
        {
            var posts = await _postRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate);
            var postAnalytics = await _postAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate);

            var hourlyEngagement = posts
                .Where(p => p.PublishedDate.HasValue)
                .Join(postAnalytics, p => p.Id, a => a.PostId, (p, a) => new { Hour = p.PublishedDate.Value.Hour, Engagement = a.EngagementRate })
                .GroupBy(x => x.Hour)
                .ToDictionary(
                    g => g.Key.ToString(),
                    g => g.Average(x => x.Engagement)
                );

            return hourlyEngagement;
        }

        private List<string> ExtractHashtags(string caption)
        {
            if (string.IsNullOrEmpty(caption)) return new List<string>();

            var hashtags = new List<string>();
            var words = caption.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            foreach (var word in words)
            {
                if (word.StartsWith("#") && word.Length > 1)
                {
                    var hashtag = word.TrimEnd('.', ',', '!', '?', ';', ':').ToLower();
                    if (!hashtags.Contains(hashtag))
                        hashtags.Add(hashtag);
                }
            }

            return hashtags;
        }

        public async Task<byte[]> ExportReportToPdfAsync(AnalyticsReportDto report)
        {
            // پیاده‌سازی تولید PDF با استفاده از کتابخانه‌هایی مثل iTextSharp یا PdfSharp
            throw new NotImplementedException("PDF export will be implemented using PDF generation library");
        }

        public async Task<byte[]> ExportReportToExcelAsync(AnalyticsReportDto report)
        {
            // پیاده‌سازی تولید Excel با استفاده از EPPlus یا ClosedXML
            throw new NotImplementedException("Excel export will be implemented using Excel generation library");
        }
    }
}
