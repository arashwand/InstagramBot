using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using InstagramBot.DTOs;
using Microsoft.Extensions.Logging;
using MatchType = InstagramBot.Core.Entities.MatchType;


namespace InstagramBot.Application.Services
{
    public class AutoReplyService : IAutoReplyService
    {
        private readonly IAutoReplyRepository _autoReplyRepository;
        private readonly IInteractionRepository _interactionRepository;
        private readonly IInstagramGraphApiClient _apiClient;
        private readonly ICustomLogService _logService;
        private readonly ILogger<AutoReplyService> _logger;

        public AutoReplyService(
            IAutoReplyRepository autoReplyRepository,
            IInteractionRepository interactionRepository,
            IInstagramGraphApiClient apiClient,
            ICustomLogService logService,
            ILogger<AutoReplyService> logger)
        {
            _autoReplyRepository = autoReplyRepository;
            _interactionRepository = interactionRepository;
            _apiClient = apiClient;
            _logService = logService;
            _logger = logger;
        }

        public async Task ProcessAutoReplyAsync(int accountId, int interactionId)
        {
            try
            {
                var interaction = await _interactionRepository.GetByIdAsync(interactionId);
                if (interaction == null || interaction.AccountId != accountId || interaction.InteractionType != "Comment" || interaction.IsReplied)
                {
                    return;
                }

                var rules = await _autoReplyRepository.GetActiveByAccountIdAsync(accountId);
                if (!rules.Any())
                {
                    return;
                }

                // مرتب‌سازی بر اساس اولویت (بالاترین اولویت اول)
                rules = rules.OrderByDescending(r => r.Priority).ToList();

                foreach (var rule in rules)
                {
                    if (await ShouldApplyRuleAsync(rule, interaction))
                    {
                        await ApplyRuleAsync(rule, interaction);
                        break; // اولین تطابق اعمال شود و متوقف شود
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing auto reply for interaction {InteractionId}", interactionId);
            }
        }

        private async Task<bool> ShouldApplyRuleAsync(AutoReplyRule rule, Interaction interaction)
        {
            if (!MatchesKeywords(rule, interaction.Content))
            {
                return false;
            }

            var repliesInLastHour = await _interactionRepository.GetAutoRepliesCountInLastHourAsync(rule.AccountId);
            if (repliesInLastHour >= rule.MaxRepliesPerHour)
            {
                _logger.LogInformation("Rate limit reached for account {AccountId}: {Count}/{Limit}", rule.AccountId, repliesInLastHour, rule.MaxRepliesPerHour);
                return false;
            }

            if (rule.DelayMinutes > 0)
            {
                var timeSinceReceived = DateTime.UtcNow - interaction.ReceivedDate;
                if (timeSinceReceived.TotalMinutes < rule.DelayMinutes)
                {
                    return false;
                }
            }

            return true;
        }

        private bool MatchesKeywords(AutoReplyRule rule, string content)
        {
            if (string.IsNullOrEmpty(content)) return false;

            var keywords = rule.Keywords ?? new List<string>();
            var contentLower = content.ToLower();

            switch (rule.MatchType)
            {
                case Core.Entities.MatchType.ContainsAny:
                    return keywords.Any(k => contentLower.Contains(k.ToLower()));
                case MatchType.ContainsAll:
                    return keywords.All(k => contentLower.Contains(k.ToLower()));
                case MatchType.ExactMatch:
                    return keywords.Any(k => contentLower == k.ToLower());
                default:
                    return false;
            }
        }

        private async Task ApplyRuleAsync(AutoReplyRule rule, Interaction interaction)
        {
            try
            {
                // نیاز به دریافت access token از account
                var account = interaction.Account; // یا از repository دریافت
                var replyId = await _apiClient.ReplyToCommentAsync(
                    interaction.InstagramCommentId,
                    rule.ReplyMessage,
                    account.AccessToken // فرض بر دسترسی به account
                );

                if (!string.IsNullOrEmpty(replyId))
                {
                    interaction.IsReplied = true;
                    interaction.ReplyContent = rule.ReplyMessage;
                    interaction.AutoReplyRuleId = rule.Id;
                    interaction.AutoReplyTime = DateTime.UtcNow;

                    await _interactionRepository.UpdateAsync(interaction);

                    await _logService.LogUserActivityAsync(interaction.AccountId, "AutoReplySent",
                        $"Auto reply sent for comment {interaction.Id} using rule {rule.Name}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error applying auto reply rule {RuleId} to interaction {InteractionId}", rule.Id, interaction.Id);
            }
        }

        public async Task<List<AutomationRuleDto>> GetAllRulesAsync(int userId)
        {
            var rules = await _autoReplyRepository.GetByUserIdAsync(userId);
            return rules.Select(rule => new AutomationRuleDto
            {
                Id = rule.Id,
                AccountId = rule.AccountId,
                Name = rule.Name,
                IsActive = rule.IsActive,
                MatchType = (DTOs.MatchType)(int)rule.MatchType,  // مطمئن شوید DTO.MatchType وجود دارد
                Keywords = rule.Keywords ?? new List<string>(),
                ReplyMessage = rule.ReplyMessage,
                Priority = rule.Priority,
                MaxRepliesPerHour = rule.MaxRepliesPerHour,
                DelayMinutes = rule.DelayMinutes
            }).ToList();
        }

        public async Task CreateRuleAsync(AutomationRuleDto ruleDto, int userId)
        {
            var rule = new AutoReplyRule
            {
                // Id نیازی به set ندارد، auto-generated
                AccountId = ruleDto.AccountId,
                UserId = userId,  // اضافه شده: از پارامتر
                Name = ruleDto.Name,
                IsActive = ruleDto.IsActive,
                MatchType = (MatchType)ruleDto.MatchType,
                Keywords = ruleDto.Keywords ?? new List<string>(),
                ReplyMessage = ruleDto.ReplyMessage,
                Priority = ruleDto.Priority,
                MaxRepliesPerHour = ruleDto.MaxRepliesPerHour,
                DelayMinutes = ruleDto.DelayMinutes,
                CreatedDate = DateTime.UtcNow,  // اضافه شده
                UpdatedDate = DateTime.UtcNow   // اضافه شده
            };

            await _autoReplyRepository.CreateAsync(rule);
        }
    }
}