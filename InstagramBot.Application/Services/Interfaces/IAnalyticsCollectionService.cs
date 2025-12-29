using InstagramBot.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IAnalyticsCollectionService
    {
        Task CollectAccountAnalyticsAsync(int accountId);
        Task CollectPostAnalyticsAsync(int postId);
        Task CollectAllAccountsAnalyticsAsync();
        Task ProcessPendingAnalyticsAsync();
        Task<AccountAnalyticsDto> GetLatestAccountAnalyticsAsync(int accountId);
        Task<List<PostAnalyticsDto>> GetPostAnalyticsAsync(int accountId, DateTime fromDate, DateTime toDate);
        Task ScheduleAnalyticsCollectionAsync();
    }
}
