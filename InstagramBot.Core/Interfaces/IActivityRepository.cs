using InstagramBot.Core.Entities;

namespace InstagramBot.Core.Interfaces
{
    public interface IActivityRepository
    {
        Task<List<Activity>> GetRecentAsync(int count);
        Task<Activity> CreateAsync(Activity activity);
        Task DeleteOldAsync(int days);
    }
}