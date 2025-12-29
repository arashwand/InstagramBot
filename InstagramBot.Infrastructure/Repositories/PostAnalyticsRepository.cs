using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using InstagramBot.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace InstagramBot.Infrastructure.Repositories
{
    public class PostAnalyticsRepository : IPostAnalyticsRepository
    {
        private readonly ApplicationDbContext _context;

        public PostAnalyticsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PostAnalytics> CreateAsync(PostAnalytics analytics)
        {
            analytics.CreatedDate = DateTime.UtcNow;
            _context.PostAnalytics.Add(analytics);
            await _context.SaveChangesAsync();
            return analytics;
        }

        public async Task<PostAnalytics> UpdateAsync(PostAnalytics analytics)
        {
            _context.PostAnalytics.Update(analytics);
            await _context.SaveChangesAsync();
            return analytics;
        }

        public async Task<PostAnalytics> GetByPostAndDateAsync(int postId, DateTime date)
        {
            return await _context.PostAnalytics
                .FirstOrDefaultAsync(p => p.PostId == postId && p.Date.Date == date.Date);
        }

        public async Task<List<PostAnalytics>> GetByAccountAndDateRangeAsync(int accountId, DateTime fromDate, DateTime toDate)
        {
            return await _context.PostAnalytics
                .Where(p => p.Post.AccountId == accountId && p.Date >= fromDate && p.Date <= toDate)
                .Include(p => p.Post)
                .ToListAsync();
        }

        public async Task<List<PostAnalytics>> GetTopPostsByEngagementAsync(int accountId, DateTime fromDate, DateTime toDate, int count)
        {
            var topPosts = await _context.PostAnalytics
                .Where(p => p.Post.AccountId == accountId && p.Date >= fromDate && p.Date <= toDate)
                .Include(p => p.Post)
                .OrderByDescending(p => p.EngagementRate)
                .Take(count)
                .ToListAsync();

            return topPosts.Select(p => new PostAnalytics
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
                EngagementRate = p.EngagementRate,
                AudienceGender = (p.AudienceGender),
                AudienceAge = (p.AudienceAge),
                AudienceCountry = (p.AudienceCountry)
            }).ToList();
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