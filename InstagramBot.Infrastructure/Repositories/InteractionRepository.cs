using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramBot.Infrastructure.Repositories
{
    public class InteractionRepository : IInteractionRepository
    {
        private readonly ApplicationDbContext _context;

        public InteractionRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Interaction> CreateAsync(Interaction interaction)
        {
            _context.Interactions.Add(interaction);
            await _context.SaveChangesAsync();
            return interaction;
        }

        public async Task<List<Interaction>> GetByAccountIdAsync(int accountId)
        {
            return await _context.Interactions
                .Where(i => i.AccountId == accountId)
                .ToListAsync();
        }

        public async Task<List<Interaction>> GetByPostIdAsync(int postId)
        {
            return await _context.Interactions
                .Where(i => i.PostId == postId)
                .ToListAsync();
        }

        public async Task<Interaction> GetByIdAsync(int id)
        {
            return await _context.Interactions.FindAsync(id);
        }

        public async Task<Interaction> UpdateAsync(Interaction interaction)
        {
            _context.Interactions.Update(interaction);
            await _context.SaveChangesAsync();
            return interaction;
        }

        public async Task<int> GetAutoRepliesCountInLastHourAsync(int accountId)
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            return await _context.Interactions
                .Where(i => i.AccountId == accountId &&
                           i.AutoReplyTime.HasValue &&
                           i.AutoReplyTime >= oneHourAgo)
                .CountAsync();
        }
    }
}