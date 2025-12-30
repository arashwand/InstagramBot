using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Interfaces;
using InstagramBot.DTOs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services
{
    public class ActivityService : IActivityService
    {
        private readonly IActivityRepository _activityRepository; // Assuming exists
        private readonly ILogger<ActivityService> _logger;

        public ActivityService(IActivityRepository activityRepository, ILogger<ActivityService> logger)
        {
            _activityRepository = activityRepository;
            _logger = logger;
        }

        public async Task<List<ActivityDto>> GetRecentActivitiesAsync(int count)
        {
            var activities = await _activityRepository.GetRecentAsync(count);
            return activities.Select(a => new ActivityDto
            {
                Id = a.Id,
                Type = a.Type,
                Message = a.Message,
                CreatedAt = a.CreatedAt
            }).ToList();
        }
    }
}