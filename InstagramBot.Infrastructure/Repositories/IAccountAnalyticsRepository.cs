using InstagramBot.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Infrastructure.Repositories
{
    public interface IAccountAnalyticsRepository
    {
        Task<AccountAnalytics> CreateAsync(AccountAnalytics analytics);
        Task<AccountAnalytics> UpdateAsync(AccountAnalytics analytics);
        Task<AccountAnalytics> GetByAccountAndDateAsync(int userId, int accountId, DateTime date);
        Task<AccountAnalytics> GetLatestByAccountIdAsync(int userId, int accountId);
        Task<List<AccountAnalytics>> GetByUserIdAndDateRangeAsync( int userId, int accountId, DateTime fromDate, DateTime toDate);
    }
}
