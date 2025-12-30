using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Interfaces;
using InstagramBot.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services
{
    public class AnalyticsService : IAnalyticsService
    {
        private readonly IPostAnalyticsRepository _postAnalyticsRepository;
        private readonly IAccountAnalyticsRepository _accountAnalyticsRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(IPostAnalyticsRepository postAnalyticsRepository, IAccountAnalyticsRepository accountAnalyticsRepository, IAccountRepository accountRepository, ILogger<AnalyticsService> logger)
        {
            _postAnalyticsRepository = postAnalyticsRepository;
            _accountAnalyticsRepository = accountAnalyticsRepository;
            _accountRepository = accountRepository;
            _logger = logger;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(int userId, string period)
        {
            var toDate = DateTime.UtcNow;
            var fromDate = GetFromDate(period);

            var dateRangeDays = (toDate - fromDate).TotalDays;
            var prevFromDate = fromDate.AddDays(-dateRangeDays);
            var prevToDate = fromDate;

            var userAccounts = await _accountRepository.GetByUserIdAsync(userId);
            var postAnalytics = await _postAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId, fromDate, toDate);
            var prevPostAnalytics = await _postAnalyticsRepository.GetByUserIdAndDateRangeAsync(userId, prevFromDate, prevToDate);

            var totalLikes = postAnalytics.Sum(p => p.LikesCount);
            var totalComments = postAnalytics.Sum(p => p.CommentsCount);
            var prevTotalLikes = prevPostAnalytics.Sum(p => p.LikesCount);
            var prevTotalComments = prevPostAnalytics.Sum(p => p.CommentsCount);

            return new DashboardStatsDto
            {
                TotalAccounts = userAccounts.Count,
                TotalPosts = postAnalytics.Count,
                TotalLikes = totalLikes,
                TotalComments = totalComments,
                AccountsChange = 0, // Logic for account change needs more context
                PostsChange = CalculateChange(postAnalytics.Count, prevPostAnalytics.Count),
                LikesChange = CalculateChange(totalLikes, prevTotalLikes),
                CommentsChange = CalculateChange(totalComments, prevTotalComments)
            };
        }

        public async Task<List<TopPostDto>> GetTopPostsAsync(int userId, int count)
        {
            var posts = await _postAnalyticsRepository.GetTopPostsByEngagementForUserAsync(userId, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, count);
            return posts.Select(p => new TopPostDto
            {
                Id = p.PostId,
                Caption = p.Post?.Caption ?? "",
                ThumbnailUrl = "", // Add logic
                AccountName = p.Post?.Account?.InstagramUsername ?? "",
                LikesCount = p.LikesCount,
                CommentsCount = p.CommentsCount,
                PublishedAt = p.Post?.PublishedDate ?? DateTime.UtcNow
            }).ToList();
        }

        private DateTime GetFromDate(string period)
        {
            return period switch
            {
                "امروز" => DateTime.UtcNow.Date,
                "هفته گذشته" => DateTime.UtcNow.AddDays(-7),
                "ماه گذشته" => DateTime.UtcNow.AddMonths(-1),
                "سه ماه گذشته" => DateTime.UtcNow.AddMonths(-3),
                _ => DateTime.UtcNow.AddDays(-7)
            };
        }

        private double CalculateChange(double current, double previous)
        {
            if (previous == 0)
            {
                return current > 0 ? 100.0 : 0.0;
            }
            return ((current - previous) / previous) * 100.0;
        }
    }
}