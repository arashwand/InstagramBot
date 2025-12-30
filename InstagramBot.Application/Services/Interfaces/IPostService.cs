using InstagramBot.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IPostService
    {
        Task<List<PostDto>> GetAllPostsAsync();
        Task<PostDto> GetPostByIdAsync(int id);
        Task CreatePostAsync(CreatePostDto post);
        Task UpdatePostAsync(int id, PostDto post);
        Task DeletePostAsync(int id);
        Task<List<ScheduledPostDto>> GetScheduledPostsAsync(int count);
    }
}