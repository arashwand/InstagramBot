using InstagramBot.Application.DTOs;
using InstagramBot.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IPostPublishingService
    {
        Task<PublishResult> PublishPostAsync(Post post, Account account);
        Task<PublishResult> PublishPostNowAsync(PostPublishDto publishDto, int userId);
        Task<PublishResult> PublishCarouselPostAsync(CarouselPostDto carouselDto, int userId);
        Task<PublishResult> PublishStoryAsync(Post post, Account account);
        Task<bool> ValidatePostContentAsync(string caption, string mediaUrl);
    }
}
