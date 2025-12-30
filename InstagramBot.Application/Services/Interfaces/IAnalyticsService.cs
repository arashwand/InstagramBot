using InstagramBot.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IAnalyticsService
    {
        Task<DashboardStatsDto> GetDashboardStatsAsync(string period);
        Task<List<TopPostDto>> GetTopPostsAsync(int count);
    }
}