using InstagramBot.Application.DTOs;
using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace InstagramBot.Application.Services
{
    public class WebhookProcessingService : IWebhookProcessingService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly IPostRepository _postRepository;
        private readonly IInteractionRepository _interactionRepository;
        private readonly IInstagramGraphApiClient _apiClient;
        private readonly ICustomLogService _logService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WebhookProcessingService> _logger;

        private readonly string _appSecret;

        public WebhookProcessingService(
            IAccountRepository accountRepository,
            IPostRepository postRepository,
            IInteractionRepository interactionRepository,
            IInstagramGraphApiClient apiClient,
            ICustomLogService logService,
            IConfiguration configuration,
            ILogger<WebhookProcessingService> logger)
        {
            _accountRepository = accountRepository;
            _postRepository = postRepository;
            _interactionRepository = interactionRepository;
            _apiClient = apiClient;
            _logService = logService;
            _configuration = configuration;
            _logger = logger;

            _appSecret = _configuration["Instagram:AppSecret"];
        }

        public async Task ProcessWebhookAsync(InstagramWebhookDto webhook, string signature)
        {
            try
            {
                _logger.LogInformation("Processing webhook with {EntryCount} entries", webhook.Entry?.Count ?? 0);

                foreach (var entry in webhook.Entry ?? new List<InstagramWebhookEntry>())
                {
                    // پردازش تغییرات (کامنت‌ها، منشن‌ها)
                    foreach (var change in entry.Changes ?? new List<InstagramWebhookChange>())
                    {
                        await ProcessChangeAsync(change);
                    }

                    // پردازش پیام‌های دایرکت
                    foreach (var messaging in entry.Messaging ?? new List<InstagramWebhookMessaging>())
                    {
                        await ProcessDirectMessageAsync(messaging);
                    }
                }

                await _logService.LogInstagramApiCallAsync(0, "ProcessWebhook", true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                await _logService.LogInstagramApiCallAsync(0, "ProcessWebhook", false, ex.Message);
                throw;
            }
        }

        public bool VerifySignature(string payload, string signature)
        {
            if (string.IsNullOrEmpty(signature) || !signature.StartsWith("sha256="))
            {
                return false;
            }

            try
            {
                var expectedSignature = signature.Substring(7); // حذف "sha256="
                var keyBytes = Encoding.UTF8.GetBytes(_appSecret);
                var payloadBytes = Encoding.UTF8.GetBytes(payload);

                using var hmac = new HMACSHA256(keyBytes);
                var computedHash = hmac.ComputeHash(payloadBytes);
                var computedSignature = Convert.ToHexString(computedHash).ToLower();

                return computedSignature == expectedSignature;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying webhook signature");
                return false;
            }
        }

        private async Task ProcessChangeAsync(InstagramWebhookChange change)
        {
            switch (change.Field)
            {
                case "comments":
                    await ProcessCommentAsync(change);
                    break;
                case "mentions":
                    await ProcessMentionAsync(change);
                    break;
                default:
                    _logger.LogInformation("Unhandled webhook field: {Field}", change.Field);
                    break;
            }
        }

        public async Task ProcessCommentAsync(InstagramWebhookChange change)
        {
            try
            {
                var value = change.Value;
                if (value == null) return;

                _logger.LogInformation("Processing comment webhook for media {MediaId}", value.MediaId);

                // پیدا کردن پست مربوطه
                var post = await _postRepository.GetByInstagramMediaIdAsync(value.MediaId);
                if (post == null)
                {
                    _logger.LogWarning("Post not found for Instagram media ID: {MediaId}", value.MediaId);
                    return;
                }

                // پیدا کردن حساب مربوطه
                var account = await _accountRepository.GetByIdAsync(post.AccountId);
                if (account == null)
                {
                    _logger.LogWarning("Account not found for post {PostId}", post.Id);
                    return;
                }

                // ایجاد رکورد تعامل
                var interaction = new Interaction
                {
                    AccountId = account.Id,
                    PostId = post.Id,
                    InteractionType = "Comment",
                    InstagramCommentId = value.Id,
                    SenderUsername = value.From?.Username,
                    SenderId = value.From?.Id,
                    Content = value.Text,
                    ReceivedDate = DateTime.UtcNow,
                    IsReplied = false
                };

                await _interactionRepository.CreateAsync(interaction);

                await _logService.LogUserActivityAsync(account.UserId, "CommentReceived",
                    $"New comment received on post {post.Id} from @{value.From?.Username}");

                _logger.LogInformation("Comment processed successfully for post {PostId}", post.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing comment webhook");
            }
        }

        public async Task ProcessMentionAsync(InstagramWebhookChange change)
        {
            try
            {
                var value = change.Value;
                if (value == null) return;

                _logger.LogInformation("Processing mention webhook for media {MediaId}", value.MediaId);

                // پیدا کردن حساب مربوطه بر اساس recipient
                var account = await _accountRepository.GetByInstagramUserIdAsync(value.Id);
                if (account == null)
                {
                    _logger.LogWarning("Account not found for Instagram user ID: {UserId}", value.Id);
                    return;
                }

                // ایجاد رکورد تعامل برای منشن
                var interaction = new Interaction
                {
                    AccountId = account.Id,
                    PostId = null, // منشن ممکن است مربوط به پست خاصی نباشد
                    InteractionType = "Mention",
                    SenderUsername = value.From?.Username,
                    SenderId = value.From?.Id,
                    Content = value.Text,
                    ReceivedDate = DateTime.UtcNow,
                    IsReplied = false
                };

                await _interactionRepository.CreateAsync(interaction);

                await _logService.LogUserActivityAsync(account.UserId, "MentionReceived",
                    $"New mention received from @{value.From?.Username}");

                _logger.LogInformation("Mention processed successfully for account {AccountId}", account.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing mention webhook");
            }
        }

        public async Task ProcessDirectMessageAsync(InstagramWebhookMessaging messaging)
        {
            try
            {
                if (messaging.Message == null) return;

                _logger.LogInformation("Processing direct message webhook from {SenderId}", messaging.Sender?.Id);

                // پیدا کردن حساب مربوطه
                var account = await _accountRepository.GetByInstagramUserIdAsync(messaging.Recipient?.Id);
                if (account == null)
                {
                    _logger.LogWarning("Account not found for Instagram user ID: {UserId}", messaging.Recipient?.Id);
                    return;
                }

                // ایجاد رکورد تعامل برای پیام دایرکت
                var interaction = new Interaction
                {
                    AccountId = account.Id,
                    PostId = null,
                    InteractionType = "DirectMessage",
                    InstagramMessageId = messaging.Message.Mid,
                    SenderId = messaging.Sender?.Id,
                    Content = messaging.Message.Text,
                    ReceivedDate = DateTimeOffset.FromUnixTimeMilliseconds(messaging.Timestamp).DateTime,
                    IsReplied = false
                };

                await _interactionRepository.CreateAsync(interaction);

                await _logService.LogUserActivityAsync(account.UserId, "DirectMessageReceived",
                    $"New direct message received from user {messaging.Sender?.Id}");

                _logger.LogInformation("Direct message processed successfully for account {AccountId}", account.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing direct message webhook");
            }
        }
    }
}

