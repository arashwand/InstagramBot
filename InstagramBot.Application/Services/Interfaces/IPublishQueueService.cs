using InstagramBot.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IPublishQueueService
    {
        Task<int> EnqueuePostAsync(int accountId, int postId, DateTime scheduledTime, string priority = "Normal");
        Task<int> EnqueueStoryAsync(int accountId, int postId, DateTime scheduledTime, string priority = "Normal");
        Task<bool> ProcessQueueItemAsync(int queueId);
        Task<List<PublishQueue>> GetQueueStatusAsync(int accountId);
        Task<bool> CancelQueueItemAsync(int queueId);
        Task<bool> CheckRateLimitAsync(int accountId, string actionType);
        Task ProcessPendingQueueAsync();
    }
}
