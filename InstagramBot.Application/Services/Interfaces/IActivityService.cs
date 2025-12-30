using InstagramBot.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IActivityService
    {
        Task<List<ActivityDto>> GetRecentActivitiesAsync(int count);
    }
}