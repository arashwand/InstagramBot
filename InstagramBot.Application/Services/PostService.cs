using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Interfaces;
using InstagramBot.DTOs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services
{
    public class PostService : IPostService
    {
        private readonly IPostRepository _postRepository;
        private readonly ILogger<PostService> _logger;

        public PostService(IPostRepository postRepository, ILogger<PostService> logger)
        {
            _postRepository = postRepository;
            _logger = logger;
        }

        public async Task<List<PostDto>> GetAllPostsAsync()
        {
            var posts = await _postRepository.GetAllAsync();
            return posts.Select(p => new PostDto
            {
                Id = p.Id,
                Caption = p.Caption,
                MediaUrls = p.MediaUrl?.Split(',').ToList() ?? new List<string>(),
                InstagramMediaId = p.InstagramMediaId,
                IsStory = p.IsStory,
                ScheduledAt = p.ScheduledDate,
                PublishedAt = p.PublishedDate,
                Status = p.Status,
                AccountId = p.AccountId
            }).ToList();
        }

        public async Task<PostDto> GetPostByIdAsync(int id)
        {
            var post = await _postRepository.GetByIdAsync(id);
            return new PostDto
            {
                Id = post.Id,
                Caption = post.Caption,
                MediaUrls = post.MediaUrl?.Split(',').ToList() ?? new List<string>(),
                InstagramMediaId = post.InstagramMediaId,
                IsStory = post.IsStory,
                ScheduledAt = post.ScheduledDate,
                PublishedAt = post.PublishedDate,
                Status = post.Status,
                AccountId = post.AccountId
            };
        }

        public async Task CreatePostAsync(CreatePostDto dto)
        {
            var post = new Core.Entities.Post
            {
                Caption = dto.Caption,
                MediaUrl = string.Join(",", dto.MediaUrls),
                InstagramMediaId = dto.InstagramMediaId,
                IsStory = dto.IsStory,
                ScheduledDate = dto.ScheduledAt.Value,
                AccountId = dto.AccountId,
                Status = "Scheduled"
            };
            await _postRepository.CreateAsync(post);
        }

        public async Task UpdatePostAsync(int id, PostDto dto)
        {
            var post = await _postRepository.GetByIdAsync(id);
            post.Caption = dto.Caption;
            post.MediaUrl = string.Join(",", dto.MediaUrls);
            post.IsStory = dto.IsStory;
            post.ScheduledDate = dto.ScheduledAt.Value;
            post.Status = dto.Status;
            await _postRepository.UpdateAsync(post);
        }

        public async Task DeletePostAsync(int id)
        {
            await _postRepository.DeleteAsync(id);
        }

        public async Task<List<ScheduledPostDto>> GetScheduledPostsAsync(int count)
        {
            var posts = await _postRepository.GetScheduledPostsAsync(count);
            return posts.Select(p => new ScheduledPostDto
            {
                Id = p.Id,
                Caption = p.Caption,
                AccountUsername = p.Account?.InstagramUsername ?? "",
                ScheduledDate = p.ScheduledDate,
                Status = p.Status
            }).ToList();
        }
    }
}