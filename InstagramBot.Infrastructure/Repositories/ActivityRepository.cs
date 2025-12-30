using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramBot.Infrastructure.Repositories
{
    public class ActivityRepository : IActivityRepository
    {
        private readonly ApplicationDbContext _context;

        public ActivityRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Core.Entities.Activity>> GetRecentAsync(int count)
        {
            return await _context.Activities
                .OrderByDescending(a => a.CreatedAt)
                .Take(count)
                .ToListAsync();
        }

        public async Task<Activity> CreateAsync(Activity activity)
        {
            _context.Activities.Add(activity);
            await _context.SaveChangesAsync();
            return activity;
        }

        public async Task DeleteOldAsync(int days)
        {
            var cutoffDate = DateTime.UtcNow.AddDays(-days);
            var oldActivities = _context.Activities.Where(a => a.CreatedAt < cutoffDate);
            _context.Activities.RemoveRange(oldActivities);
            await _context.SaveChangesAsync();
        }
    }
}