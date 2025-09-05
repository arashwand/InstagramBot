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
        Task<Post> GetByInstagramMediaIdAsync(string instagramMediaId);
        Task<Post> CreateAsync(Post post);
        Task<Post> UpdateAsync(Post post);
        Task<List<Post>> GetByAccountIdAsync(int accountId);
        Task<Post> GetByIdAsync(int postId);
    }

}
