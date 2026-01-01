using InstagramBot.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace InstagramBot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public DashboardController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService;
        }

        [HttpGet("stats")]
        public async Task<IActionResult> GetDashboardStats([FromQuery] string period = "هفته گذشته")
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            {
                return Unauthorized("Invalid user identifier.");
            }
            var stats = await _analyticsService.GetDashboardStatsAsync(userId, period);
            return Ok(stats);
        }

        [HttpGet("top-posts")]
        public async Task<IActionResult> GetTopPosts([FromQuery] int count = 5)
        {
            if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
            {
                return Unauthorized("Invalid user identifier.");
            }
            var posts = await _analyticsService.GetTopPostsAsync(userId, count);
            return Ok(posts);
        }
    }
}
