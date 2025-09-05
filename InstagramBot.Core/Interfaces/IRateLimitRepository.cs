using InstagramBot.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Interfaces
{
    public interface IRateLimitRepository
    {
        Task<RateLimitTracker> CreateAsync(RateLimitTracker tracker);
        Task<int> GetActionCountAsync(int accountId, string actionType, DateTime from, DateTime to);
        Task<RateLimitTracker> GetLastActionAsync(int accountId, string actionType);
        Task<RateLimitTracker> GetOldestActionInPeriodAsync(int accountId, string actionType, DateTime from);
    }
}
