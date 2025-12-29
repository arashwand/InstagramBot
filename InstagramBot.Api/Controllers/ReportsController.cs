using InstagramBot.Application.Services;
using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InstagramBot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportsController : ControllerBase
    {
        private readonly IReportingService _reportingService;
        private readonly IAnalyticsCollectionService _analyticsService;
        private readonly IAccountRepository _accountRepository;

        public ReportsController(
            IReportingService reportingService,
            IAnalyticsCollectionService analyticsService,
            IAccountRepository accountRepository)
        {
            _reportingService = reportingService;
            _analyticsService = analyticsService;
            _accountRepository = accountRepository;
        }

        [HttpGet("account/{accountId}")]
        public async Task<IActionResult> GetAccountReport(int accountId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                // اعتبارسنجی مالکیت حساب
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || account.UserId != userId)
                {
                    return NotFound("حساب یافت نشد.");
                }

                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var to = toDate ?? DateTime.UtcNow;

                var report = await _reportingService.GenerateAccountReportAsync(accountId, from, to);
                return Ok(report);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("account/{accountId}/top-posts")]
        public async Task<IActionResult> GetTopPosts(int accountId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate, [FromQuery] int count = 10)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || account.UserId != userId)
                {
                    return NotFound("حساب یافت نشد.");
                }

                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var to = toDate ?? DateTime.UtcNow;

                var topPosts = await _reportingService.GetTopPerformingPostsAsync(accountId, from, to, count);
                return Ok(topPosts);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("account/{accountId}/engagement-trends")]
        public async Task<IActionResult> GetEngagementTrends(int accountId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || account.UserId != userId)
                {
                    return NotFound("حساب یافت نشد.");
                }

                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var to = toDate ?? DateTime.UtcNow;

                var trends = await _reportingService.GetEngagementTrendsAsync(accountId, from, to);
                return Ok(trends);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("account/{accountId}/audience-insights")]
        public async Task<IActionResult> GetAudienceInsights(int accountId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || account.UserId != userId)
                {
                    return NotFound("حساب یافت نشد.");
                }

                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var to = toDate ?? DateTime.UtcNow;

                var insights = await _reportingService.GetAudienceInsightsAsync(accountId, from, to);
                return Ok(insights);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("account/{accountId}/best-posting-times")]
        public async Task<IActionResult> GetBestPostingTimes(int accountId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || account.UserId != userId)
                {
                    return NotFound("حساب یافت نشد.");
                }

                var bestTimes = await _reportingService.GetBestPostingTimesAsync(accountId);
                return Ok(bestTimes);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("account/{accountId}/hashtag-performance")]
        public async Task<IActionResult> GetHashtagPerformance(int accountId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || account.UserId != userId)
                {
                    return NotFound("حساب یافت نشد.");
                }

                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var to = toDate ?? DateTime.UtcNow;

                var performance = await _reportingService.GetHashtagPerformanceAsync(accountId, from, to);
                return Ok(performance);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("account/{accountId}/collect-analytics")]
        public async Task<IActionResult> CollectAnalytics(int accountId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || account.UserId != userId)
                {
                    return NotFound("حساب یافت نشد.");
                }

                await _analyticsService.CollectAccountAnalyticsAsync(accountId);
                return Ok(new { Message = "جمع‌آوری آمار با موفقیت آغاز شد." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("account/{accountId}/export/pdf")]
        public async Task<IActionResult> ExportReportToPdf(int accountId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || account.UserId != userId)
                {
                    return NotFound("حساب یافت نشد.");
                }

                var from = fromDate ?? DateTime.UtcNow.AddDays(-30);
                var to = toDate ?? DateTime.UtcNow;

                var report = await _reportingService.GenerateAccountReportAsync(accountId, from, to);
                var pdfBytes = await _reportingService.ExportReportToPdfAsync(report);

                return File(pdfBytes, "application/pdf", $"report_{account.InstagramUsername}_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf");
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
