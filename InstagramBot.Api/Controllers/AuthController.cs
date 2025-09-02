using InstagramBot.Application.DTOs;
using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace InstagramBot.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly IJwtService _jwtService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            IJwtService jwtService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _jwtService = jwtService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                _logger.LogInformation("User {Username} registered successfully", user.Username);
                var token = _jwtService.GenerateToken(user);
                return Ok(new { Token = token, User = new { user.Id, user.Username, user.Email } });
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return BadRequest(ModelState);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByNameAsync(model.Username);
            if (user == null)
            {
                return Unauthorized("نام کاربری یا رمز عبور اشتباه است.");
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (result.Succeeded)
            {
                user.LastLoginDate = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                var token = _jwtService.GenerateToken(user);
                _logger.LogInformation("User {Username} logged in successfully", user.Username);

                return Ok(new { Token = token, User = new { user.Id, user.Username, user.Email } });
            }

            if (result.RequiresTwoFactor)
            {
                return Ok(new { RequiresTwoFactor = true, UserId = user.Id });
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("User {Username} account locked out", model.Username);
                return Unauthorized("حساب کاربری شما به دلیل تلاش‌های ناموفق متعدد قفل شده است.");
            }

            return Unauthorized("نام کاربری یا رمز عبور اشتباه است.");
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto model)
        {
            var principal = _jwtService.ValidateToken(model.Token);
            if (principal == null)
                return Unauthorized("توکن نامعتبر است.");

            var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null || !user.IsActive)
                return Unauthorized("کاربر یافت نشد یا غیرفعال است.");

            var newToken = _jwtService.GenerateToken(user);
            return Ok(new { Token = newToken });
        }
    }
}
