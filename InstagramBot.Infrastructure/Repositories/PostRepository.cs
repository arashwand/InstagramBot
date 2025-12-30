
﻿using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramBot.Infrastructure.Repositories
{
    public class PostRepository : IPostRepository
    {
        private readonly ApplicationDbContext _context;

        public PostRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Post> CreateAsync(Post post)
        {
            post.CreatedDate = DateTime.UtcNow;
            _context.Posts.Add(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<Post> UpdateAsync(Post post)
        {
            _context.Posts.Update(post);
            await _context.SaveChangesAsync();
            return post;
        }

        public async Task<Post> GetByIdAsync(int postId)
        {
            return await _context.Posts.FindAsync(postId);
        }

        public async Task<Post> GetByInstagramMediaIdAsync(string instagramMediaId)
        {
            return await _context.Posts
                .FirstOrDefaultAsync(p => p.InstagramMediaId == instagramMediaId);
        }

        public async Task<List<Post>> GetByAccountIdAsync(int accountId)
        {
            return await _context.Posts
                .Where(p => p.AccountId == accountId)
                .ToListAsync();
        }

        public async Task<List<Post>> GetRecentPublishedPostsAsync(int accountId, int days)
        {
            var sinceDate = DateTime.UtcNow.AddDays(-days);
            return await _context.Posts
                .Where(p => p.AccountId == accountId &&
                           p.PublishedDate.HasValue &&
                           p.PublishedDate >= sinceDate)
                .OrderByDescending(p => p.PublishedDate)
                .ToListAsync();
        }

        public async Task<List<Post>> GetPublishedPostsAsync(int accountId)
        {
            return await _context.Posts
                .Where(p => p.AccountId == accountId && p.PublishedDate.HasValue)
                .OrderByDescending(p => p.PublishedDate)
                .ToListAsync();
        }

        public async Task<List<Post>> GetScheduledPostsByUserIdAsync(int userId, int? accountId = null)
        {
            var query = _context.Posts
                .Where(p => p.Account.UserId == userId && p.Status == "Scheduled")
                .Include(p => p.Account);

            if (accountId.HasValue)
            {
                query = (Microsoft.EntityFrameworkCore.Query.IIncludableQueryable<Post, Account>)query.Where(p => p.AccountId == accountId.Value);
            }

            return await query
                .OrderBy(p => p.ScheduledDate)
                .ToListAsync();
        }

        public async Task<List<Post>> GetByAccountAndDateRangeAsync(int accountId, DateTime fromDate, DateTime toDate)
        {
            return await _context.Posts
                .Where(p => p.AccountId == accountId &&
                            p.PublishedDate.HasValue &&
                            p.PublishedDate >= fromDate &&
                            p.PublishedDate <= toDate)
                .Select(p => new Post
                {
                    Id = p.Id,
                    InstagramMediaId = p.InstagramMediaId,
                    Caption = p.Caption,
                    PublishedDate = p.PublishedDate,
                    LikesCount = p.LikesCount,
                    CommentsCount = p.CommentsCount
                })
                .ToListAsync();
        }

        public async Task<List<Post>> GetScheduledPostsAsync(int count)
        {
            return await _context.Posts
                .Where(p => p.ScheduledDate > DateTime.UtcNow && p.Status == "Scheduled")
                .Include(p => p.Account)
                .OrderBy(p => p.ScheduledDate)
                .Take(count)
                .ToListAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                _context.Posts.Remove(post);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<List<Post>> GetAllAsync()
        {
            return await _context.Posts
                .Include(p => p.Account)
                .OrderByDescending(p => p.CreatedDate)
                .ToListAsync();
        }
    }
}