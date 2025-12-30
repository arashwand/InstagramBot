using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramBot.Infrastructure.Repositories
{
    public class AutoReplyRepository : IAutoReplyRepository
    {
        private readonly ApplicationDbContext _context;

        public AutoReplyRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AutoReplyRule> CreateAsync(AutoReplyRule rule)
        {
            _context.AutoReplyRules.Add(rule);
            await _context.SaveChangesAsync();
            return rule;
        }

        public async Task<AutoReplyRule> UpdateAsync(AutoReplyRule rule)
        {
            _context.AutoReplyRules.Update(rule);
            await _context.SaveChangesAsync();
            return rule;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var rule = await _context.AutoReplyRules.FindAsync(id);
            if (rule == null) return false;
            _context.AutoReplyRules.Remove(rule);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<AutoReplyRule> GetByIdAsync(int id)
        {
            return await _context.AutoReplyRules.FindAsync(id);
        }

        public async Task<List<AutoReplyRule>> GetByAccountIdAsync(int accountId)
        {
            return await _context.AutoReplyRules
                .Where(r => r.AccountId == accountId)
                .ToListAsync();
        }

        public async Task<List<AutoReplyRule>> GetActiveByAccountIdAsync(int accountId)
        {
            return await _context.AutoReplyRules
                .Where(r => r.AccountId == accountId && r.IsActive)
                .ToListAsync();
        }

        public async Task<List<AutoReplyRule>> GetByUserIdAsync(int userId)
        {
            return await _context.AutoReplyRules
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<AutoReplyRule>> GetActiveByUserIdAsync(int userId)
        {
            return await _context.AutoReplyRules
                .Where(r => r.UserId == userId && r.IsActive)
                .ToListAsync();
        }
    }
}