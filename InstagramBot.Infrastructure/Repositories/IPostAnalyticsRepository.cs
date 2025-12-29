using InstagramBot.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Infrastructure.Repositories
{
    public interface IPostAnalyticsRepository
    {
        Task<PostAnalytics> CreateAsync(PostAnalytics analytics);
        Task<PostAnalytics> UpdateAsync(PostAnalytics analytics);
        Task<PostAnalytics> GetByPostAndDateAsync(int postId, DateTime date);
        Task<List<PostAnalytics>> GetByAccountAndDateRangeAsync(int accountId, DateTime fromDate, DateTime toDate);
        Task<List<PostAnalytics>> GetTopPostsByEngagementAsync(int accountId, DateTime fromDate, DateTime toDate, int count);
    }
}
