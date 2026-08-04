using InstagramBot.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IPostService
    {
        Task<List<PostDto>> GetAllPostsAsync(int userId);
        Task<PostDto> GetPostByIdAsync(int id, int userId);
        Task CreatePostAsync(CreatePostDto post, int userId);
        Task UpdatePostAsync(int userId, PostDto post);
        Task DeletePostAsync(int id, int userId);
        Task<List<ScheduledPostDto>> GetScheduledPostsAsync(int count, int userId);
    }
}