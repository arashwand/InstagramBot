using InstagramBot.Application.Services;
using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Interfaces;
using InstagramBot.DTOs;
using InstagramBot.Infrastructure.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Globalization;
using InstagramBot.Core.Entities;

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

        public async Task<AnalyticsReportDto> GenerateAccountReportAsync(int userId, int accountId, DateTime fromDate, DateTime toDate)
        {
            try
            {
                _logger.LogInformation("Generating report for account {AccountId} from {FromDate} to {ToDate}",
                    accountId, fromDate, toDate);

                var account = await _accountRepository.GetByIdAsync(accountId, userId);
                if (account == null)
                    throw new ArgumentException("حساب یافت نشد یا شما به آن دسترسی ندارید.");

                // دریافت آمار حساب
                var accountAnalytics = await _accountAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId, fromDate, toDate);
                var postAnalytics = await _postAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId, fromDate, toDate);

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

                var mapTopPosts = topPosts.Select(p => new TopPostDto
                {
                    Id = p.PostId,  // شناسه پست
                    Caption = p.Post?.Caption ?? "",  // کپشن از Post
                    ThumbnailUrl = p.Post?.MediaUrl?.Split(',').FirstOrDefault() ?? "",  // اولین رسانه به عنوان thumbnail
                    AccountName = p.Post?.Account?.InstagramUsername ?? "",  // نام اکانت از Account
                    LikesCount = p.LikesCount,
                    CommentsCount = p.CommentsCount,
                    PublishedAt = p.Post?.PublishedDate ?? p.Date  // تاریخ انتشار از Post یا Date
                }).ToList();

                // آمار پست‌ها بر اساس روز
                var postsByDay = await GetPostsByDayAsync(userId, accountId, fromDate, toDate);

                // آمار تعامل بر اساس ساعت
                var engagementByHour = await GetEngagementByHourAsync(userId, accountId, fromDate, toDate);

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
                    TopPosts = mapTopPosts,
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

        public async Task<List<PostAnalytics>> GetTopPerformingPostsAsync(int userId, int accountId, DateTime fromDate, DateTime toDate, int count = 10)
        {
            // Note: The userId is passed to the repository, but the current implementation of GetTopPostsByEngagementForUserAsync
            // filters by userId, not accountId. If filtering by a specific account for a user is needed, the repo method should be updated.
            return await _postAnalyticsRepository.GetTopPostsByEngagementForUserAsync(userId, fromDate, toDate, count);

            //return topPosts.Select(p => new PostAnalyticsDto
            //{
            //    PostId = p.PostId,
            //    InstagramMediaId = p.Post?.InstagramMediaId,
            //    Date = p.Date,
            //    Impressions = p.Impressions,
            //    Reach = p.Reach,
            //    LikesCount = p.LikesCount,
            //    CommentsCount = p.CommentsCount,
            //    SavesCount = p.SavesCount,
            //    SharesCount = p.SharesCount,
            //    VideoViews = p.VideoViews,
            //    ProfileVisits = p.ProfileVisits,
            //    EngagementRate = p.EngagementRate
            //}).ToList();
        }

        public async Task<Dictionary<string, object>> GetEngagementTrendsAsync(int userId, int accountId, DateTime fromDate, DateTime toDate)
        {
            var postAnalytics = await _postAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId, fromDate, toDate);

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

        public async Task<Dictionary<string, object>> GetAudienceInsightsAsync(int userId, int accountId, DateTime fromDate, DateTime toDate)
        {
            var postAnalytics = await _postAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId, fromDate, toDate);

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

        public async Task<Dictionary<string, object>> GetBestPostingTimesAsync(int userId, int accountId)
        {
            var posts = await _postRepository.GetPublishedPostsAsync(accountId, userId);
            var postAnalytics = await _postAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId, DateTime.UtcNow.AddDays(-90), DateTime.UtcNow);

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

        public async Task<Dictionary<string, object>> GetHashtagPerformanceAsync(int userId, int accountId, DateTime fromDate, DateTime toDate)
        {
            var posts = await _postRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate, userId);
            var postAnalytics = await _postAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId, fromDate, toDate);

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

        public async Task<Dictionary<string, object>> GetCompetitorAnalysisAsync(int userId, int accountId, List<string> competitorUsernames)
        {
            // این بخش نیاز به دسترسی به اطلاعات عمومی رقبا دارد
            // که ممکن است محدودیت‌هایی داشته باشد

            var account = await _accountRepository.GetByIdAsync(accountId, userId);
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

        private async Task<Dictionary<string, int>> GetPostsByDayAsync(int userId, int accountId, DateTime fromDate, DateTime toDate)
        {
            var posts = await _postRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate, userId);

            return posts
                .Where(p => p.PublishedDate.HasValue)
                .GroupBy(p => p.PublishedDate.Value.Date)
                .OrderBy(g => g.Key)
                .ToDictionary(
                    g => g.Key.ToString("yyyy-MM-dd"),
                    g => g.Count()
                );
        }

        private async Task<Dictionary<string, double>> GetEngagementByHourAsync(int userId, int accountId, DateTime fromDate, DateTime toDate)
        {
            var posts = await _postRepository.GetByAccountAndDateRangeAsync(accountId, fromDate, toDate, userId);
            var postAnalytics = await _postAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId, fromDate, toDate);

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
            try
            {
                using (var document = new PdfDocument())
                {
                    var page = document.AddPage();
                    var gfx = XGraphics.FromPdfPage(page);
                    var font = new XFont("Arial", 12,XFontStyleEx.Regular);
                    var titleFont = new XFont("Arial", 16, XFontStyleEx.Bold);

                    double yPosition = 50;

                    // عنوان
                    gfx.DrawString("گزارش تحلیل اینستاگرام", titleFont, XBrushes.Black, new XRect(0, yPosition, page.Width, 30), XStringFormats.TopCenter);
                    yPosition += 40;

                    // اطلاعات دوره
                    gfx.DrawString($"از تاریخ: {report.FromDate:yyyy/MM/dd} تا {report.ToDate:yyyy/MM/dd}", font, XBrushes.Black, 50, yPosition);
                    yPosition += 20;

                    // آمار کلی
                    gfx.DrawString($"کل پست‌ها: {report.TotalPosts}", font, XBrushes.Black, 50, yPosition);
                    yPosition += 20;
                    gfx.DrawString($"کل استوری‌ها: {report.TotalStories}", font, XBrushes.Black, 50, yPosition);
                    yPosition += 20;
                    gfx.DrawString($"کل نمایش‌ها: {report.TotalImpressions:N0}", font, XBrushes.Black, 50, yPosition);
                    yPosition += 20;
                    gfx.DrawString($"کل دسترسی‌ها: {report.TotalReach:N0}", font, XBrushes.Black, 50, yPosition);
                    yPosition += 20;
                    gfx.DrawString($"کل لایک‌ها: {report.TotalLikes:N0}", font, XBrushes.Black, 50, yPosition);
                    yPosition += 20;
                    gfx.DrawString($"کل کامنت‌ها: {report.TotalComments:N0}", font, XBrushes.Black, 50, yPosition);
                    yPosition += 20;
                    gfx.DrawString($"میانگین تعامل: {report.AverageEngagementRate:F2}%", font, XBrushes.Black, 50, yPosition);
                    yPosition += 20;
                    gfx.DrawString($"رشد فالوور: {report.FollowersGrowth:N0}", font, XBrushes.Black, 50, yPosition);
                    yPosition += 40;

                    // بهترین پست‌ها
                    if (report.TopPosts != null && report.TopPosts.Any())
                    {
                        gfx.DrawString("بهترین پست‌ها:", titleFont, XBrushes.Black, 50, yPosition);
                        yPosition += 20;
                        foreach (var post in report.TopPosts.Take(5))
                        {
                            gfx.DrawString($"پست {post.Id}: تعامل {post.EngagementRate:F2}%", font, XBrushes.Black, 50, yPosition);
                            yPosition += 15;
                        }
                    }

                    using (var stream = new MemoryStream())
                    {
                        document.Save(stream, false);
                        return stream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report to PDF");
                throw;
            }
        }

        public async Task<byte[]> ExportReportToExcelAsync(AnalyticsReportDto report)
        {
            try
            {
                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add("گزارش تحلیل");

                    // عنوان
                    worksheet.Cells[1, 1].Value = "گزارش تحلیل اینستاگرام";
                    worksheet.Cells[1, 1].Style.Font.Size = 16;
                    worksheet.Cells[1, 1].Style.Font.Bold = true;

                    // اطلاعات دوره
                    worksheet.Cells[3, 1].Value = "از تاریخ";
                    worksheet.Cells[3, 2].Value = report.FromDate.ToString("yyyy/MM/dd");
                    worksheet.Cells[4, 1].Value = "تا تاریخ";
                    worksheet.Cells[4, 2].Value = report.ToDate.ToString("yyyy/MM/dd");

                    // آمار کلی
                    worksheet.Cells[6, 1].Value = "آمار کلی";
                    worksheet.Cells[6, 1].Style.Font.Bold = true;
                    worksheet.Cells[7, 1].Value = "کل پست‌ها";
                    worksheet.Cells[7, 2].Value = report.TotalPosts;
                    worksheet.Cells[8, 1].Value = "کل استوری‌ها";
                    worksheet.Cells[8, 2].Value = report.TotalStories;
                    worksheet.Cells[9, 1].Value = "کل نمایش‌ها";
                    worksheet.Cells[9, 2].Value = report.TotalImpressions;
                    worksheet.Cells[10, 1].Value = "کل دسترسی‌ها";
                    worksheet.Cells[10, 2].Value = report.TotalReach;
                    worksheet.Cells[11, 1].Value = "کل لایک‌ها";
                    worksheet.Cells[11, 2].Value = report.TotalLikes;
                    worksheet.Cells[12, 1].Value = "کل کامنت‌ها";
                    worksheet.Cells[12, 2].Value = report.TotalComments;
                    worksheet.Cells[13, 1].Value = "میانگین تعامل";
                    worksheet.Cells[13, 2].Value = $"{report.AverageEngagementRate:F2}%";
                    worksheet.Cells[14, 1].Value = "رشد فالوور";
                    worksheet.Cells[14, 2].Value = report.FollowersGrowth;

                    // بهترین پست‌ها
                    if (report.TopPosts != null && report.TopPosts.Any())
                    {
                        worksheet.Cells[16, 1].Value = "بهترین پست‌ها";
                        worksheet.Cells[16, 1].Style.Font.Bold = true;
                        worksheet.Cells[17, 1].Value = "شناسه پست";
                        worksheet.Cells[17, 2].Value = "نرخ تعامل";

                        int row = 18;
                        foreach (var post in report.TopPosts.Take(10))
                        {
                            worksheet.Cells[row, 1].Value = post.Id;
                            worksheet.Cells[row, 2].Value = $"{post.EngagementRate:F2}%";
                            row++;
                        }
                    }

                    // آمار پست‌ها بر اساس روز (اگر موجود باشد)
                    if (report.PostsByDay != null && report.PostsByDay.Any())
                    {
                        var postsSheet = package.Workbook.Worksheets.Add("پست‌ها بر اساس روز");
                        postsSheet.Cells[1, 1].Value = "تاریخ";
                        postsSheet.Cells[1, 2].Value = "تعداد پست";

                        int row = 2;
                        foreach (var item in report.PostsByDay)
                        {
                            postsSheet.Cells[row, 1].Value = item.Key;
                            postsSheet.Cells[row, 2].Value = item.Value;
                            row++;
                        }
                    }

                    // تنظیم عرض ستون‌ها
                    worksheet.Column(1).Width = 20;
                    worksheet.Column(2).Width = 15;

                    using (var stream = new MemoryStream())
                    {
                        package.SaveAs(stream);
                        return stream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting report to Excel");
                throw;
            }
        }
    }
}