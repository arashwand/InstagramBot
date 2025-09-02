namespace InstagramBot.Application.Services.Interfaces
{
    public interface ITokenManagementService
    {
        Task RefreshExpiredTokensAsync();
        Task RefreshTokenForAccountAsync(int accountId);
        Task ValidateAllTokensAsync();
        Task ScheduleTokenRefreshAsync();
    }
}