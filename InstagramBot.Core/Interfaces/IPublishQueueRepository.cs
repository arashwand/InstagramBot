using InstagramBot.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Interfaces
{
    public interface IPublishQueueRepository
    {
        Task<PublishQueue> CreateAsync(PublishQueue queue);
        Task<PublishQueue> GetByIdAsync(int id);
        Task<PublishQueue> UpdateAsync(PublishQueue queue);
        Task<List<PublishQueue>> GetByAccountIdAsync(int accountId);
        Task<List<PublishQueue>> GetPendingItemsAsync(DateTime upToTime);
    }
}
