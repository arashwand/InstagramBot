using InstagramBot.Core.Entities;
using InstagramBot.DTOs;
using System.Security.Principal;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IInstagramOAuthService
    {
        string GenerateAuthorizationUrl(int userId);
        Task<Account> HandleCallbackAsync(int userId, InstagramCallbackDto callback);
        Task<string> RefreshTokenAsync(string accessToken);
        Task<bool> ValidateTokenAsync(string accessToken);

    }
}
