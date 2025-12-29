using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace InstagramBot.Application.Services
{
    public class PerformanceComparisonService : IPerformanceComparisonService
    {
        private readonly IAccountAnalyticsRepository _accountAnalyticsRepository;
        private readonly IPostAnalyticsRepository _postAnalyticsRepository;
        private readonly IReportingService _reportingService;
        private readonly ILogger<PerformanceComparisonService> _logger;

        public PerformanceComparisonService(
            IAccountAnalyticsRepository accountAnalyticsRepository,
            IPostAnalyticsRepository postAnalyticsRepository,
            IReportingService reportingService,
            ILogger<PerformanceComparisonService> logger)
        {
            _accountAnalyticsRepository = accountAnalyticsRepository;
            _postAnalyticsRepository = postAnalyticsRepository;
            _reportingService = reportingService;
            _logger = logger;
        }

        public async Task<Dictionary<string, object>> ComparePeriodsAsync(int accountId, DateTime period1Start, DateTime period1End, DateTime period2Start, DateTime period2End)
        {
            try
            {
                // دریافت آمار دوره اول
                var period1Analytics = await _postAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, period1Start, period1End);
                var period1Account = await _accountAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, period1Start, period1End);

                // دریافت آمار دوره دوم
                var period2Analytics = await _postAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, period2Start, period2End);
                var period2Account = await _accountAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, period2Start, period2End);

                // محاسبه آمار دوره اول
                var period1Stats = CalculatePeriodStats(period1Analytics, period1Account);
                var period2Stats = CalculatePeriodStats(period2Analytics, period2Account);

                // محاسبه تغییرات
                var comparison = new Dictionary<string, object>
                {
                    ["period1"] = new
                    {
                        StartDate = period1Start,
                        EndDate = period1End,
                        Stats = period1Stats
                    },
                    ["period2"] = new
                    {
                        StartDate = period2Start,
                        EndDate = period2End,
                        Stats = period2Stats
                    },
                    ["changes"] = CalculateChanges(period1Stats, period2Stats),
                    ["insights"] = GenerateInsights(period1Stats, period2Stats)
                };

                return comparison;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing periods for account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetPerformanceScoreAsync(int accountId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var postAnalytics = await _postAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate);
                var accountAnalytics = await _accountAnalyticsRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate);

                if (!postAnalytics.Any() || !accountAnalytics.Any())
                {
                    return new Dictionary<string, object>
                    {
                        ["overallScore"] = 0,
                        ["message"] = "داده‌های کافی برای محاسبه امتیاز موجود نیست."
                    };
                }

                // محاسبه امتیازهای مختلف (از 0 تا 100)
                var engagementScore = CalculateEngagementScore(postAnalytics);
                var consistencyScore = CalculateConsistencyScore(postAnalytics);
                var growthScore = CalculateGrowthScore(accountAnalytics);
                var contentQualityScore = CalculateContentQualityScore(postAnalytics);

                var overallScore = (engagementScore + consistencyScore + growthScore + contentQualityScore) / 4;

                return new Dictionary<string, object>
                {
                    ["overallScore"] = Math.Round(overallScore, 1),
                    ["breakdown"] = new
                    {
                        Engagement = Math.Round(engagementScore, 1),
                        Consistency = Math.Round(consistencyScore, 1),
                        Growth = Math.Round(growthScore, 1),
                        ContentQuality = Math.Round(contentQualityScore, 1)
                    },
                    ["recommendations"] = GenerateRecommendations(engagementScore, consistencyScore, growthScore, contentQualityScore),
                    ["grade"] = GetPerformanceGrade(overallScore)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating performance score for account {AccountId}", accountId);
                throw;
            }
        }

        private Dictionary<string, double> CalculatePeriodStats(List<PostAnalytics> postAnalytics, List<AccountAnalytics> accountAnalytics)
        {
            return new Dictionary<string, double>
            {
                ["totalPosts"] = postAnalytics.Count,
                ["totalImpressions"] = postAnalytics.Sum(p => p.Impressions),
                ["totalReach"] = postAnalytics.Sum(p => p.Reach),
                ["totalLikes"] = postAnalytics.Sum(p => p.LikesCount),
                ["totalComments"] = postAnalytics.Sum(p => p.CommentsCount),
                ["averageEngagement"] = postAnalytics.Any() ? postAnalytics.Average(p => p.EngagementRate) : 0,
                ["followersGrowth"] = CalculateFollowersGrowth(accountAnalytics)
            };
        }

        private Dictionary<string, object> CalculateChanges(Dictionary<string, double> period1, Dictionary<string, double> period2)
        {
            var changes = new Dictionary<string, object>();

            foreach (var key in period1.Keys)
            {
                if (period2.ContainsKey(key))
                {
                    var change = period1[key] != 0 ? ((period2[key] - period1[key]) / period1[key]) * 100 : 0;
                    changes[key] = new
                    {
                        Value = Math.Round(change, 2),
                        Direction = change > 0 ? "up" : change < 0 ? "down" : "stable",
                        Percentage = $"{Math.Abs(change):F1}%"
                    };
                }
            }

            return changes;
        }

        private double CalculateEngagementScore(List<PostAnalytics> analytics)
        {
            if (!analytics.Any()) return 0;

            var avgEngagement = analytics.Average(p => p.EngagementRate);

            // امتیاز بر اساس نرخ تعامل (معیارهای صنعت)
            if (avgEngagement >= 6) return 100;
            if (avgEngagement >= 4) return 80;
            if (avgEngagement >= 2) return 60;
            if (avgEngagement >= 1) return 40;
            return 20;
        }

        private double CalculateConsistencyScore(List<PostAnalytics> analytics)
        {
            if (analytics.Count < 7) return 0; // حداقل یک هفته پست

            // محاسبه انحراف معیار نرخ تعامل
            var engagementRates = analytics.Select(p => p.EngagementRate).ToList();
            var mean = engagementRates.Average();
            var variance = engagementRates.Sum(x => Math.Pow(x - mean, 2)) / engagementRates.Count;
            var standardDeviation = Math.Sqrt(variance);

            // امتیاز بر اساس ثبات (انحراف معیار کمتر = امتیاز بیشتر)
            var consistencyScore = Math.Max(0, 100 - (standardDeviation * 10));
            return Math.Min(100, consistencyScore);
        }

        private double CalculateGrowthScore(List<AccountAnalytics> analytics)
        {
            if (analytics.Count < 2) return 50; // امتیاز متوسط اگر داده کافی نباشد

            var firstDay = analytics.OrderBy(a => a.Date).First();
            var lastDay = analytics.OrderByDescending(a => a.Date).First();

            var growthRate = firstDay.FollowersCount != 0
                ? ((double)(lastDay.FollowersCount - firstDay.FollowersCount) / firstDay.FollowersCount) * 100
                : 0;

            // امتیاز بر اساس نرخ رشد
            if (growthRate >= 10) return 100;
            if (growthRate >= 5) return 80;
            if (growthRate >= 2) return 60;
            if (growthRate >= 0) return 40;
            return 20;
        }

        private double CalculateContentQualityScore(List<PostAnalytics> analytics)
        {
            if (!analytics.Any()) return 0;

            // معیارهای کیفیت محتوا
            var avgSaves = analytics.Average(p => p.SavesCount);
            var avgShares = analytics.Average(p => p.SharesCount);
            var avgCommentToLikeRatio = analytics.Average(p =>
                p.LikesCount > 0 ? (double)p.CommentsCount / p.LikesCount : 0);

            // امتیاز ترکیبی بر اساس ذخیره، اشتراک و نسبت کامنت به لایک
            var saveScore = Math.Min(100, avgSaves * 2);
            var shareScore = Math.Min(100, avgShares * 5);
            var commentRatioScore = Math.Min(100, avgCommentToLikeRatio * 1000);

            return (saveScore + shareScore + commentRatioScore) / 3;
        }

        private List<string> GenerateRecommendations(double engagement, double consistency, double growth, double quality)
        {
            var recommendations = new List<string>();

            if (engagement < 60)
                recommendations.Add("بهبود استراتژی تعامل با مخاطبان");

            if (consistency < 60)
                recommendations.Add("حفظ ثبات در کیفیت و زمان‌بندی محتوا");

            if (growth < 60)
                recommendations.Add("تقویت استراتژی‌های جذب فالوور جدید");

            if (quality < 60)
                recommendations.Add("افزایش کیفیت و ارزش محتوای تولیدی");

            if (!recommendations.Any())
                recommendations.Add("عملکرد شما عالی است! ادامه دهید.");

            return recommendations;
        }

        private string GetPerformanceGrade(double score)
        {
            if (score >= 90) return "A+";
            if (score >= 80) return "A";
            if (score >= 70) return "B+";
            if (score >= 60) return "B";
            if (score >= 50) return "C+";
            if (score >= 40) return "C";
            return "D";
        }

        private double CalculateFollowersGrowth(List<AccountAnalytics> analytics)
        {
            if (analytics.Count < 2) return 0;

            var first = analytics.OrderBy(a => a.Date).First();
            var last = analytics.OrderByDescending(a => a.Date).First();

            return first.FollowersCount != 0
                ? ((double)(last.FollowersCount - first.FollowersCount) / first.FollowersCount) * 100
                : 0;
        }

        private List<string> GenerateInsights(Dictionary<string, double> period1, Dictionary<string, double> period2)
        {
            var insights = new List<string>();

            // مقایسه تعامل
            var engagementChange = period2["averageEngagement"] - period1["averageEngagement"];
            if (engagementChange > 0.5)
                insights.Add($"نرخ تعامل {engagementChange:F1}% بهبود یافته است.");
            else if (engagementChange < -0.5)
                insights.Add($"نرخ تعامل {Math.Abs(engagementChange):F1}% کاهش یافته است.");

            // مقایسه تعداد پست
            var postsChange = period2["totalPosts"] - period1["totalPosts"];
            if (postsChange > 0)
                insights.Add($"{postsChange} پست بیشتر منتشر شده است.");
            else if (postsChange < 0)
                insights.Add($"{Math.Abs(postsChange)} پست کمتر منتشر شده است.");

            return insights;
        }

        public Task<Dictionary<string, object>> CompareBenchmarkAsync(int accountId, string industry)
        {
            throw new NotImplementedException();
        }

        public Task<List<Dictionary<string, object>>> GetCompetitorComparisonAsync(int accountId, List<string> competitorUsernames)
        {
            throw new NotImplementedException();
        }
    }
}
