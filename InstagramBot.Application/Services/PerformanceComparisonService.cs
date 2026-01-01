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

        public async Task<Dictionary<string, object>> ComparePeriodsAsync(int userId, int accountId, DateTime period1Start, DateTime period1End, DateTime period2Start, DateTime period2End)
        {
            try
            {
                // دریافت آمار دوره اول
                var period1Analytics = await _postAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId, period1Start, period1End);
                var period1Account = await _accountAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId,accountId, period1Start, period1End);

                // دریافت آمار دوره دوم
                var period2Analytics = await _postAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId, period2Start, period2End);
                var period2Account = await _accountAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId,accountId, period2Start, period2End);

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

        public async Task<Dictionary<string, object>> CompareBenchmarkAsync(int userId, int accountId, string industry)
        {
            try
            {
                // دریافت داده‌های عملکرد حساب
                var accountData = await GetAccountPerformanceData(userId,accountId);

                // دریافت میانگین صنعت
                var industryBenchmarks = GetIndustryBenchmarks(industry);

                // مقایسه
                var comparison = new Dictionary<string, object>
                {
                    ["accountPerformance"] = accountData,
                    ["industryBenchmarks"] = industryBenchmarks,
                    ["comparison"] = CompareWithBenchmark(accountData, industryBenchmarks)
                };

                return comparison;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error comparing benchmark for account {AccountId} in industry {Industry}", accountId, industry);
                throw;
            }
        }

        public async Task<Dictionary<string, object>> GetPerformanceScoreAsync(int userId, int accountId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                var postAnalytics = await _postAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId, fromDate, toDate);
                var accountAnalytics = await _accountAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId,accountId, fromDate, toDate);

                if (!postAnalytics.Any() || !accountAnalytics.Any())
                {
                    return new Dictionary<string, object>
                    {
                        ["overallScore"] = 0,
                        ["message"] = "داده‌های کافی برای محاسبه امتیاز موجود نیست."
                    };
                }

                var engagement = postAnalytics.Average(p => p.EngagementRate);
                var consistency = CalculateConsistency(postAnalytics);
                var growth = CalculateFollowersGrowth(accountAnalytics);
                var quality = CalculateContentQuality(postAnalytics);

                var overallScore = (engagement + consistency + growth + quality) / 4;

                var score = new Dictionary<string, object>
                {
                    ["overallScore"] = Math.Round(overallScore, 2),
                    ["engagement"] = Math.Round(engagement, 2),
                    ["consistency"] = Math.Round(consistency, 2),
                    ["growth"] = Math.Round(growth, 2),
                    ["quality"] = Math.Round(quality, 2),
                    ["grade"] = GetPerformanceGrade(overallScore),
                    ["recommendations"] = GenerateRecommendations(engagement, consistency, growth, quality)
                };

                return score;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating performance score for account {AccountId}", accountId);
                throw;
            }
        }

        public async Task<List<Dictionary<string, object>>> GetCompetitorComparisonAsync(int userId, int accountId, List<string> competitorUsernames)
        {
            try
            {
                var comparisons = new List<Dictionary<string, object>>();

                // دریافت داده‌های حساب خود
                var ownData = await GetAccountPerformanceData(userId, accountId);

                foreach (var competitor in competitorUsernames)
                {
                    // شبیه‌سازی دریافت داده‌های رقیب (در عمل باید از API یا دیتابیس گرفته شود)
                    var competitorData = await GetCompetitorData(competitor);

                    var comparison = new Dictionary<string, object>
                    {
                        ["competitorUsername"] = competitor,
                        ["ownPerformance"] = ownData,
                        ["competitorPerformance"] = competitorData,
                        ["comparison"] = CompareCompetitor(ownData, competitorData)
                    };

                    comparisons.Add(comparison);
                }

                return comparisons;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting competitor comparison for account {AccountId}", accountId);
                throw;
            }
        }

        private Dictionary<string, double> CalculatePeriodStats(List<PostAnalytics> postAnalytics, List<AccountAnalytics> accountAnalytics)
        {
            var stats = new Dictionary<string, double>
            {
                ["averageEngagement"] = postAnalytics.Any() ? postAnalytics.Average(p => p.EngagementRate) : 0,
                ["totalPosts"] = postAnalytics.Count,
                ["averageImpressions"] = postAnalytics.Any() ? postAnalytics.Average(p => p.Impressions) : 0,
                ["averageReach"] = postAnalytics.Any() ? postAnalytics.Average(p => p.Reach) : 0,
                ["followersCount"] = accountAnalytics.Any() ? accountAnalytics.Last().FollowersCount : 0
            };

            return stats;
        }

        private Dictionary<string, double> CalculateChanges(Dictionary<string, double> period1, Dictionary<string, double> period2)
        {
            var changes = new Dictionary<string, double>();

            foreach (var key in period1.Keys)
            {
                if (period1[key] != 0)
                {
                    changes[key] = ((period2[key] - period1[key]) / period1[key]) * 100;
                }
                else
                {
                    changes[key] = 0;
                }
            }

            return changes;
        }

        private double CalculateConsistency(List<PostAnalytics> analytics)
        {
            if (!analytics.Any()) return 0;

            var engagementRates = analytics.Select(a => a.EngagementRate).ToList();
            var average = engagementRates.Average();
            var variance = engagementRates.Sum(rate => Math.Pow(rate - average, 2)) / engagementRates.Count;

            // Consistency score: lower variance = higher consistency
            return Math.Max(0, 100 - variance);
        }

        private double CalculateContentQuality(List<PostAnalytics> analytics)
        {
            if (!analytics.Any()) return 0;

            // Quality based on engagement per impression
            var qualityScores = analytics.Select(a => a.Impressions > 0 ? (a.EngagementRate / a.Impressions) * 100 : 0).ToList();
            return qualityScores.Average();
        }

        private double CalculateSaveScore(List<PostAnalytics> analytics)
        {
            if (!analytics.Any()) return 0;

            var saveRates = analytics.Select(a => a.Reach > 0 ? ((double)a.SavesCount / a.Reach) * 100 : 0).ToList();
            return saveRates.Average();
        }

        private double CalculateShareScore(List<PostAnalytics> analytics)
        {
            if (!analytics.Any()) return 0;

            var shareRates = analytics.Select(a => a.Reach > 0 ? ((double)a.SharesCount / a.Reach) * 100 : 0).ToList();
            return shareRates.Average();
        }

        private double CalculateCommentRatio(List<PostAnalytics> analytics)
        {
            if (!analytics.Any()) return 0;

            var commentRatios = analytics.Select(a => a.Reach > 0 ? ((double)a.CommentsCount / a.Reach) * 100 : 0).ToList();
            return commentRatios.Average();
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

        private async Task<Dictionary<string, double>> GetAccountPerformanceData(int userId, int accountId)
        {
            // دریافت داده‌های اخیر (مثلاً 30 روز گذشته)
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-30);

            var postAnalytics = await _postAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId, startDate, endDate);
            var accountAnalytics = await _accountAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId,accountId, startDate, endDate);

            return new Dictionary<string, double>
            {
                ["engagementRate"] = postAnalytics.Any() ? postAnalytics.Average(p => p.EngagementRate) : 0,
                ["followersGrowth"] = CalculateFollowersGrowth(accountAnalytics),
                ["contentQuality"] = CalculateContentQuality(postAnalytics),
                ["consistency"] = CalculateConsistency(postAnalytics)
            };
        }

        private Dictionary<string, double> GetIndustryBenchmarks(string industry)
        {
            // میانگین‌های استاندارد صنعت بر اساس داده‌های عمومی Instagram (قابل تنظیم)
            switch (industry.ToLower())
            {
                case "fashion":
                    return new Dictionary<string, double>
                    {
                        ["engagementRate"] = 2.5, // %
                        ["followersGrowth"] = 1.2, // %
                        ["contentQuality"] = 1.8,
                        ["consistency"] = 75
                    };
                case "food":
                    return new Dictionary<string, double>
                    {
                        ["engagementRate"] = 3.0,
                        ["followersGrowth"] = 1.5,
                        ["contentQuality"] = 2.2,
                        ["consistency"] = 80
                    };
                case "travel":
                    return new Dictionary<string, double>
                    {
                        ["engagementRate"] = 2.8,
                        ["followersGrowth"] = 1.3,
                        ["contentQuality"] = 2.0,
                        ["consistency"] = 78
                    };
                default: // general
                    return new Dictionary<string, double>
                    {
                        ["engagementRate"] = 2.0,
                        ["followersGrowth"] = 1.0,
                        ["contentQuality"] = 1.5,
                        ["consistency"] = 70
                    };
            }
        }

        private Dictionary<string, object> CompareWithBenchmark(Dictionary<string, double> account, Dictionary<string, double> benchmark)
        {
            var comparison = new Dictionary<string, object>();

            foreach (var key in account.Keys)
            {
                if (benchmark.ContainsKey(key))
                {
                    var difference = account[key] - benchmark[key];
                    var status = difference > 0 ? "بالاتر از میانگین" : difference < 0 ? "پایین‌تر از میانگین" : "متوسط";
                    comparison[key] = new
                    {
                        AccountValue = Math.Round(account[key], 2),
                        BenchmarkValue = benchmark[key],
                        Difference = Math.Round(difference, 2),
                        Status = status
                    };
                }
            }

            return comparison;
        }

        private async Task<Dictionary<string, double>> GetCompetitorData(string username)
        {
            // شبیه‌سازی داده‌های رقیب (در پیاده‌سازی واقعی باید از Instagram API یا دیتابیس گرفته شود)
            // برای سادگی، مقادیر تصادفی نزدیک به میانگین تولید می‌کنیم
            var random = new Random(username.GetHashCode());

            return new Dictionary<string, double>
            {
                ["engagementRate"] = random.Next(15, 40) / 10.0, // 1.5 to 4.0
                ["followersGrowth"] = random.Next(5, 25) / 10.0, // 0.5 to 2.5
                ["contentQuality"] = random.Next(10, 30) / 10.0, // 1.0 to 3.0
                ["consistency"] = random.Next(60, 90) // 60 to 90
            };
        }

        private Dictionary<string, object> CompareCompetitor(Dictionary<string, double> own, Dictionary<string, double> competitor)
        {
            var comparison = new Dictionary<string, object>();

            foreach (var key in own.Keys)
            {
                if (competitor.ContainsKey(key))
                {
                    var difference = own[key] - competitor[key];
                    var status = difference > 0 ? "بهتر" : difference < 0 ? "ضعیف‌تر" : "مشابه";
                    comparison[key] = new
                    {
                        OwnValue = Math.Round(own[key], 2),
                        CompetitorValue = Math.Round(competitor[key], 2),
                        Difference = Math.Round(difference, 2),
                        Status = status
                    };
                }
            }

            return comparison;
        }
    }
}