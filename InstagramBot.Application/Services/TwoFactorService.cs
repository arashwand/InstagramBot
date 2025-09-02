using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using QRCoder;
using System.Drawing;
using System.Drawing.Imaging;

namespace InstagramBot.Application.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        private readonly UserManager<User> _userManager;
        private readonly IConfiguration _configuration;

        public TwoFactorService(UserManager<User> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<string> GenerateSetupCodeAsync(User user)
        {
            var key = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(key))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                key = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var appName = _configuration["AppSettings:ApplicationName"];
            var setupCode = $"otpauth://totp/{appName}:{user.Email}?secret={key}&issuer={appName}";
            return setupCode;
        }

        public async Task<byte[]> GenerateQrCodeAsync(User user)
        {
            if (user == null)
                throw new ArgumentNullException(nameof(user));

            var setupCode = await GenerateSetupCodeAsync(user).ConfigureAwait(false);

            if (string.IsNullOrEmpty(setupCode))
                throw new InvalidOperationException("Generated setup code is null or empty");

            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(setupCode, QRCodeGenerator.ECCLevel.Q);

            // استفاده از PngByteQRCode برای کارایی بهتر و حافظه کمتر
            var pngQrCode = new PngByteQRCode(qrCodeData);
            return pngQrCode.GetGraphic(20);

            //var setupCode = await GenerateSetupCodeAsync(user);

            //using var qrGenerator = new QRCodeGenerator();
            //var qrCodeData = qrGenerator.CreateQrCode(setupCode, QRCodeGenerator.ECCLevel.Q);
            //var qrCode = new QRCode(qrCodeData);

            //using var qrCodeImage = qrCode.GetGraphic(20);
            //using var stream = new MemoryStream();
            //qrCodeImage.Save(stream, ImageFormat.Png);
            //return stream.ToArray();
        }

        public async Task<bool> ValidateCodeAsync(User user, string code)
        {
            var result = await _userManager.VerifyTwoFactorTokenAsync(
                user,
                _userManager.Options.Tokens.AuthenticatorTokenProvider,
                code);
            return result;
        }

        public async Task<bool> EnableTwoFactorAsync(User user, string code)
        {
            var isValid = await ValidateCodeAsync(user, code);
            if (!isValid)
                return false;

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            user.TwoFactorEnabled = true;
            await _userManager.UpdateAsync(user);

            return true;
        }

        public async Task<bool> DisableTwoFactorAsync(User user)
        {
            await _userManager.SetTwoFactorEnabledAsync(user, false);
            user.TwoFactorEnabled = false;
            await _userManager.UpdateAsync(user);
            return true;
        }
    }
}
