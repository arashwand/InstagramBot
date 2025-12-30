using InstagramBot.Core.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Infrastructure.Repositories
{
    public class AccountAnalyticsRepository : IAccountAnalyticsRepository
    {
        private readonly ApplicationDbContext _context;

        public AccountAnalyticsRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AccountAnalytics> CreateAsync(AccountAnalytics analytics)
        {
            analytics.CreatedDate = DateTime.UtcNow;
            _context.AccountAnalytics.Add(analytics);
            await _context.SaveChangesAsync();
            return analytics;
        }

        public async Task<AccountAnalytics> UpdateAsync(AccountAnalytics analytics)
        {
            _context.AccountAnalytics.Update(analytics);
            await _context.SaveChangesAsync();
            return analytics;
        }

        public async Task<AccountAnalytics> GetByAccountAndDateAsync(int userId, int accountId, DateTime date)
        {
            return await _context.AccountAnalytics
                .FirstOrDefaultAsync(a => a.AccountId == accountId && a.Account.UserId == userId && a.Date.Date == date.Date);
        }

        public async Task<List<AccountAnalytics>> GetByUserIdAndDateRangeAsync(int userId, int accountId, DateTime fromDate, DateTime toDate)
        {
            return await _context.AccountAnalytics
                .Where(a => a.AccountId == accountId && a.Account.UserId == userId && a.Date >= fromDate && a.Date <= toDate)
                .ToListAsync();
        }

        public async Task<AccountAnalytics> GetLatestByAccountIdAsync(int userId, int accountId)
        {
            return await _context.AccountAnalytics
                .Where(a => a.AccountId == accountId && a.Account.UserId == userId)
                .OrderByDescending(a => a.Date)
                .FirstOrDefaultAsync();
        }
    }
}
