using InstagramBot.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Interfaces
{
    public interface IPostRepository
    {
        Task<List<Post>> GetAllAsync(int userId);
        Task<Post> GetByInstagramMediaIdAsync(int userId, string instagramMediaId);
        Task<Post> CreateAsync(Post post);
        Task<Post> UpdateAsync(Post post);
        Task<List<Post>> GetByAccountIdAsync(int userId, int accountId);
        Task<Post> GetByIdAsync(int userId, int postId);
        Task<List<Post>> GetRecentPublishedPostsAsync(int userId, int accountId, int days);
        Task<List<Post>> GetScheduledPostsByUserIdAsync(int userId, int? accountId = null);
        Task<List<Post>> GetPublishedPostsAsync(int userId, int accountId);
        Task<List<Post>> GetByAccountAndDateRangeAsync(int userId, int accountId, DateTime fromDate, DateTime toDate);
        Task<List<Post>> GetScheduledPostsAsync(int userId, int count);
        Task DeleteAsync(int userId, int id);
    }

}
