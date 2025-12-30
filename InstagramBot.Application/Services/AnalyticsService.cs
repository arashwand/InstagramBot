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
        private readonly ILogger<AnalyticsService> _logger;

        public AnalyticsService(IPostAnalyticsRepository postAnalyticsRepository, IAccountAnalyticsRepository accountAnalyticsRepository, ILogger<AnalyticsService> logger)
        {
            _postAnalyticsRepository = postAnalyticsRepository;
            _accountAnalyticsRepository = accountAnalyticsRepository;
            _logger = logger;
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(string period)
        {
            var fromDate = GetFromDate(period);
            var toDate = DateTime.UtcNow;

            var postAnalytics = await _postAnalyticsRepository.GetByAccountAndDateRangeAsync(0, fromDate, toDate); // Assuming account context
            var accountAnalytics = await _accountAnalyticsRepository.GetByAccountAndDateRangeAsync(0, fromDate, toDate);

            return new DashboardStatsDto
            {
                TotalAccounts = 15, // Calculate from data
                TotalPosts = postAnalytics.Count,
                TotalLikes = postAnalytics.Sum(p => p.LikesCount),
                TotalComments = postAnalytics.Sum(p => p.CommentsCount),
                AccountsChange = 12.5, // Calculate change
                PostsChange = 8.3,
                LikesChange = 15.7,
                CommentsChange = -2.1
            };
        }

        public async Task<List<TopPostDto>> GetTopPostsAsync(int count)
        {
            var posts = await _postAnalyticsRepository.GetTopPostsByEngagementAsync(0, DateTime.UtcNow.AddDays(-30), DateTime.UtcNow, count);
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
    }
}