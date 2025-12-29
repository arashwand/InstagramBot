using InstagramBot.DTOs;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IInstagramGraphApiClient
    {
        Task<List<InstagramMediaDto>> GetMediaAsync(string instagramUserId, string accessToken, int limit = 25);
        Task<InstagramMediaDto> GetMediaByIdAsync(string mediaId, string accessToken);
        Task<List<InstagramCommentDto>> GetMediaCommentsAsync(string mediaId, string accessToken);
        Task<string> ReplyToCommentAsync(string commentId, string message, string accessToken);
        Task<bool> DeleteCommentAsync(string commentId, string accessToken);
        Task<bool> HideCommentAsync(string commentId, string accessToken);
        Task<string> CreateMediaAsync(string instagramUserId, CreateMediaDto media, string accessToken);
        Task<InstagramMediaDto> PublishMediaAsync(string instagramUserId, string creationId, string accessToken);
        Task<string> CreateStoryAsync(string instagramUserId, InstagramStoryDto story, string accessToken);
        Task<List<InstagramInsightsDto>> GetMediaInsightsAsync(string mediaId, string accessToken);
        Task<List<InstagramInsightsDto>> GetAccountInsightsAsync(string instagramUserId, string accessToken, DateTime since, DateTime until);
        Task<Dictionary<string, object>> GetAccountInfoAsync(string instagramUserId, string accessToken);
    }
}