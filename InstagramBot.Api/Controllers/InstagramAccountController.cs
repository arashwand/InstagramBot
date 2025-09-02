using InstagramBot.Application.DTOs;
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
    public class InstagramAccountController : ControllerBase
    {
        private readonly IInstagramOAuthService _oauthService;
        private readonly IAccountRepository _accountRepository;
        private readonly ITokenManagementService _tokenService;
        private readonly ICustomLogService _logService;

        public InstagramAccountController(
            IInstagramOAuthService oauthService,
            IAccountRepository accountRepository,
            ITokenManagementService tokenService,
            ICustomLogService logService)
        {
            _oauthService = oauthService;
            _accountRepository = accountRepository;
            _tokenService = tokenService;
            _logService = logService;
        }

        [HttpGet("connect")]
        public IActionResult GetConnectUrl()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var authUrl = _oauthService.GenerateAuthorizationUrl(userId);

            return Ok(new { AuthorizationUrl = authUrl });
        }

        [HttpPost("callback")]
        public async Task<IActionResult> HandleCallback([FromBody] InstagramCallbackDto callback)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var account = await _oauthService.HandleCallbackAsync(userId, callback);
                return Ok(new { Message = "حساب اینستاگرام با موفقیت متصل شد.", Account = account });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("accounts")]
        public async Task<IActionResult> GetUserAccounts()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
            var accounts = await _accountRepository.GetByUserIdAsync(userId);

            return Ok(accounts.Select(a => new
            {
                a.Id,
                a.InstagramUserId,
                a.InstagramUsername,
                a.IsActive,
                a.CreatedDate,
                a.LastRefreshed,
                ExpiresAt = a.LastRefreshed.AddSeconds(a.ExpiresIn)
            }));
        }

        [HttpPost("accounts/{accountId}/refresh-token")]
        public async Task<IActionResult> RefreshToken(int accountId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                // بررسی مالکیت حساب
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || account.UserId != userId)
                {
                    return NotFound("حساب یافت نشد.");
                }

                await _tokenService.RefreshTokenForAccountAsync(accountId);
                return Ok(new { Message = "توکن با موفقیت رفرش شد." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpDelete("accounts/{accountId}")]
        public async Task<IActionResult> DisconnectAccount(int accountId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                // بررسی مالکیت حساب
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || account.UserId != userId)
                {
                    return NotFound("حساب یافت نشد.");
                }

                await _accountRepository.DeleteAsync(accountId);

                await _logService.LogUserActivityAsync(userId, "InstagramAccountDisconnected",
                    $"Disconnected Instagram account: {account.InstagramUsername}");

                return Ok(new { Message = "حساب اینستاگرام با موفقیت قطع شد." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("accounts/{accountId}/status")]
        public async Task<IActionResult> GetAccountStatus(int accountId)
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);

            try
            {
                var account = await _accountRepository.GetByIdAsync(accountId);
                if (account == null || account.UserId != userId)
                {
                    return NotFound("حساب یافت نشد.");
                }

                var isValid = await _oauthService.ValidateTokenAsync(account.AccessToken);
                var expiresAt = account.LastRefreshed.AddSeconds(account.ExpiresIn);
                var daysUntilExpiry = (expiresAt - DateTime.UtcNow).TotalDays;

                return Ok(new
                {
                    IsValid = isValid,
                    IsActive = account.IsActive,
                    ExpiresAt = expiresAt,
                    DaysUntilExpiry = Math.Max(0, daysUntilExpiry),
                    LastRefreshed = account.LastRefreshed
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
