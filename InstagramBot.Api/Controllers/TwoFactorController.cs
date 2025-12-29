using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using InstagramBot.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InstagramBot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TwoFactorController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ITwoFactorService _twoFactorService;
        private readonly IJwtService _jwtService;

        public TwoFactorController(
            UserManager<User> userManager,
            ITwoFactorService twoFactorService,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _twoFactorService = twoFactorService;
            _jwtService = jwtService;
        }

        [HttpGet("setup")]
        public async Task<IActionResult> GetSetupInfo()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            var setupCode = await _twoFactorService.GenerateSetupCodeAsync(user);
            var qrCode = await _twoFactorService.GenerateQrCodeAsync(user);

            return Ok(new
            {
                SetupCode = setupCode,
                QrCode = Convert.ToBase64String(qrCode),
                IsEnabled = user.TwoFactorEnabled,
                Success = true
            });
        }

        [HttpPost("enable")]
        public async Task<IActionResult> EnableTwoFactor([FromBody] EnableTwoFactorDto model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            var result = await _twoFactorService.EnableTwoFactorAsync(user, model.Code);

            if (result)
            {
                return Ok(new { Message = "ورود دو مرحله‌ای با موفقیت فعال شد." });
            }

            return BadRequest("کد وارد شده نامعتبر است.");
        }

        [HttpPost("disable")]
        public async Task<IActionResult> DisableTwoFactor()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return NotFound();

            await _twoFactorService.DisableTwoFactorAsync(user);
            return Ok(new { Message = "ورود دو مرحله‌ای غیرفعال شد." });
        }

        [HttpPost("verify")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyTwoFactor([FromBody] VerifyTwoFactorDto model)
        {
            var user = await _userManager.FindByIdAsync(model.UserId.ToString());
            if (user == null)
                return NotFound();

            var result = await _twoFactorService.ValidateCodeAsync(user, model.Code);

            if (result)
            {
                user.LastLoginDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                var token = _jwtService.GenerateToken(user);
                return Ok(new { Token = token, User = new { user.Id, user.Username, user.Email } });
            }

            return BadRequest("کد وارد شده نامعتبر است.");
        }
    }
}
