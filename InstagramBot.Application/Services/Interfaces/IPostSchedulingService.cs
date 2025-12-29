using InstagramBot.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IPostSchedulingService
    {
        Task<ScheduledPostDto> SchedulePostAsync(SchedulePostDto scheduleDto, int userId);
        Task<ScheduledPostDto> UpdateScheduledPostAsync(int postId, UpdateScheduledPostDto updateDto, int userId);
        Task<bool> CancelScheduledPostAsync(int postId, int userId);
        Task<List<ScheduledPostDto>> GetScheduledPostsAsync(int userId, int? accountId = null);
        Task<ScheduledPostDto> GetScheduledPostByIdAsync(int postId, int userId);
        Task PublishScheduledPostAsync(int postId);
    }
}
