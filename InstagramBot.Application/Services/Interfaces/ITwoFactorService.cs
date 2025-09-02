using InstagramBot.Core.Entities;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface ITwoFactorService
    {
        Task<string> GenerateSetupCodeAsync(User user);
        Task<byte[]> GenerateQrCodeAsync(User user);
        Task<bool> ValidateCodeAsync(User user, string code);
        Task<bool> EnableTwoFactorAsync(User user, string code);
        Task<bool> DisableTwoFactorAsync(User user);
    }
}
