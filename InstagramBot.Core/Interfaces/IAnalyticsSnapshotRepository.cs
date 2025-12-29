using InstagramBot.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Interfaces
{
    public interface IAnalyticsSnapshotRepository
    {
        Task<AnalyticsSnapshot> CreateAsync(AnalyticsSnapshot snapshot);
        Task<AnalyticsSnapshot> UpdateAsync(AnalyticsSnapshot snapshot);
        Task<List<AnalyticsSnapshot>> GetPendingSnapshotsAsync();
        Task<List<AnalyticsSnapshot>> GetByAccountAndDateRangeAsync(int accountId, DateTime fromDate, DateTime toDate);
    }
}

