using InstagramBot.DTOs;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IWebhookProcessingService
    {
        Task ProcessWebhookAsync(InstagramWebhookDto webhook, string signature);
        bool VerifySignature(string payload, string signature);
        Task ProcessCommentAsync(InstagramWebhookChange change);
        Task ProcessMentionAsync(InstagramWebhookChange change);
        Task ProcessDirectMessageAsync(InstagramWebhookMessaging messaging);
    }
}