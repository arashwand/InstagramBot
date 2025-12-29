using Hangfire;
using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using InstagramBot.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services
{
    public class PostSchedulingService : IPostSchedulingService
    {
        private readonly IPostRepository _postRepository;
        private readonly IAccountRepository _accountRepository;
        private readonly IMediaFileRepository _mediaRepository;
        private readonly IPostPublishingService _publishingService;
        private readonly ICustomLogService _logService;
        private readonly ILogger<PostSchedulingService> _logger;

        public PostSchedulingService(
            IPostRepository postRepository,
            IAccountRepository accountRepository,
            IMediaFileRepository mediaRepository,
            IPostPublishingService publishingService,
            ICustomLogService logService,
            ILogger<PostSchedulingService> logger)
        {
            _postRepository = postRepository;
            _accountRepository = accountRepository;
            _mediaRepository = mediaRepository;
            _publishingService = publishingService;
            _logService = logService;
            _logger = logger;
        }

        public async Task<ScheduledPostDto> SchedulePostAsync(SchedulePostDto scheduleDto, int userId)
        {
            try
            {
                // اعتبارسنجی حساب
                var account = await _accountRepository.GetByIdAsync(scheduleDto.AccountId);
                if (account == null || account.UserId != userId)
                {
                    throw new UnauthorizedAccessException("حساب یافت نشد یا شما مجاز به استفاده از آن نیستید.");
                }

                // اعتبارسنجی فایل رسانه
                var mediaFile = await _mediaRepository.GetByIdAsync(scheduleDto.MediaFileId);
                if (mediaFile == null || mediaFile.UserId != userId)
                {
                    throw new ArgumentException("فایل رسانه یافت نشد.");
                }

                // اعتبارسنجی زمان زمان‌بندی
                if (scheduleDto.ScheduledDate <= DateTime.UtcNow)
                {
                    throw new ArgumentException("زمان زمان‌بندی باید در آینده باشد.");
                }

                // ایجاد پست
                var post = new Post
                {
                    AccountId = scheduleDto.AccountId,
                    MediaType = mediaFile.MediaType,
                    Caption = scheduleDto.Caption,
                    MediaUrl = await GetMediaPublicUrlAsync(mediaFile),
                    ThumbnailUrl = await GetThumbnailPublicUrlAsync(mediaFile),
                    ScheduledDate = scheduleDto.ScheduledDate,
                    Status = "Scheduled",
                    IsStory = scheduleDto.IsStory,
                    StoryLink = scheduleDto.StoryLink,
                    CreatedDate = DateTime.UtcNow
                };

                var savedPost = await _postRepository.CreateAsync(post);

                // زمان‌بندی Job در Hangfire
                var jobId = BackgroundJob.Schedule<IPostSchedulingService>(
                    service => service.PublishScheduledPostAsync(savedPost.Id),
                    scheduleDto.ScheduledDate);

                // ذخیره Job ID
                savedPost.HangfireJobId = jobId;
                await _postRepository.UpdateAsync(savedPost);

                await _logService.LogUserActivityAsync(userId, "PostScheduled",
                    $"Post scheduled for {scheduleDto.ScheduledDate} on account {account.InstagramUsername}");

                _logger.LogInformation("Post scheduled successfully: {PostId} for user {UserId}",
                    savedPost.Id, userId);

                return await MapToScheduledPostDto(savedPost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error scheduling post for user {UserId}", userId);
                throw;
            }
        }

        public async Task<ScheduledPostDto> UpdateScheduledPostAsync(int postId, UpdateScheduledPostDto updateDto, int userId)
        {
            try
            {
                var post = await _postRepository.GetByIdAsync(postId);
                if (post == null)
                {
                    throw new ArgumentException("پست یافت نشد.");
                }

                var account = await _accountRepository.GetByIdAsync(post.AccountId);
                if (account.UserId != userId)
                {
                    throw new UnauthorizedAccessException("شما مجاز به ویرایش این پست نیستید.");
                }

                if (post.Status != "Scheduled")
                {
                    throw new InvalidOperationException("فقط پست‌های زمان‌بندی‌شده قابل ویرایش هستند.");
                }

                // به‌روزرسانی اطلاعات پست
                post.Caption = updateDto.Caption ?? post.Caption;
                post.StoryLink = updateDto.StoryLink ?? post.StoryLink;

                // اگر زمان تغییر کرده باشد
                if (updateDto.ScheduledDate != post.ScheduledDate && updateDto.ScheduledDate > DateTime.UtcNow)
                {
                    // لغو Job قبلی
                    if (!string.IsNullOrEmpty(post.HangfireJobId))
                    {
                        BackgroundJob.Delete(post.HangfireJobId);
                    }

                    // ایجاد Job جدید
                    var newJobId = BackgroundJob.Schedule<IPostSchedulingService>(
                        service => service.PublishScheduledPostAsync(post.Id),
                        updateDto.ScheduledDate);

                    post.ScheduledDate = updateDto.ScheduledDate;
                    post.HangfireJobId = newJobId;
                }

                var updatedPost = await _postRepository.UpdateAsync(post);

                await _logService.LogUserActivityAsync(userId, "PostUpdated",
                    $"Scheduled post {postId} updated");

                return await MapToScheduledPostDto(updatedPost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating scheduled post {PostId} for user {UserId}", postId, userId);
                throw;
            }
        }

        public async Task<bool> CancelScheduledPostAsync(int postId, int userId)
        {
            try
            {
                var post = await _postRepository.GetByIdAsync(postId);
                if (post == null)
                    return false;

                var account = await _accountRepository.GetByIdAsync(post.AccountId);
                if (account.UserId != userId)
                    return false;

                if (post.Status != "Scheduled")
                    return false;

                // لغو Job در Hangfire
                if (!string.IsNullOrEmpty(post.HangfireJobId))
                {
                    BackgroundJob.Delete(post.HangfireJobId);
                }

                // تغییر وضعیت پست
                post.Status = "Canceled";
                await _postRepository.UpdateAsync(post);

                await _logService.LogUserActivityAsync(userId, "PostCanceled",
                    $"Scheduled post {postId} canceled");

                _logger.LogInformation("Scheduled post canceled: {PostId} by user {UserId}", postId, userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error canceling scheduled post {PostId} for user {UserId}", postId, userId);
                return false;
            }
        }

        public async Task<List<ScheduledPostDto>> GetScheduledPostsAsync(int userId, int? accountId = null)
        {
            var posts = await _postRepository.GetScheduledPostsByUserIdAsync(userId, accountId);
            var result = new List<ScheduledPostDto>();

            foreach (var post in posts)
            {
                result.Add(await MapToScheduledPostDto(post));
            }

            return result;
        }

        public async Task<ScheduledPostDto> GetScheduledPostByIdAsync(int postId, int userId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
                return null;

            var account = await _accountRepository.GetByIdAsync(post.AccountId);
            if (account.UserId != userId)
                return null;

            return await MapToScheduledPostDto(post);
        }

        [AutomaticRetry(Attempts = 3)]
        public async Task PublishScheduledPostAsync(int postId)
        {
            try
            {
                _logger.LogInformation("Publishing scheduled post: {PostId}", postId);

                var post = await _postRepository.GetByIdAsync(postId);
                if (post == null || post.Status != "Scheduled")
                {
                    _logger.LogWarning("Post {PostId} not found or not scheduled", postId);
                    return;
                }

                var account = await _accountRepository.GetByIdAsync(post.AccountId);
                if (account == null || !account.IsActive)
                {
                    _logger.LogWarning("Account {AccountId} not found or inactive", post.AccountId);
                    post.Status = "Failed";
                    post.ErrorMessage = "حساب اینستاگرام غیرفعال یا یافت نشد.";
                    await _postRepository.UpdateAsync(post);
                    return;
                }

                // انتشار پست
                var publishResult = await _publishingService.PublishPostAsync(post, account);

                if (publishResult.Success)
                {
                    post.Status = "Published";
                    post.PublishedDate = DateTime.UtcNow;
                    post.InstagramMediaId = publishResult.InstagramMediaId;

                    await _logService.LogUserActivityAsync(account.UserId, "PostPublished",
                        $"Scheduled post {postId} published successfully on {account.InstagramUsername}");
                }
                else
                {
                    post.Status = "Failed";
                    post.ErrorMessage = publishResult.ErrorMessage;

                    await _logService.LogUserActivityAsync(account.UserId, "PostPublishFailed",
                        $"Failed to publish scheduled post {postId}: {publishResult.ErrorMessage}");
                }

                await _postRepository.UpdateAsync(post);

                _logger.LogInformation("Scheduled post processing completed: {PostId}, Status: {Status}",
                    postId, post.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing scheduled post {PostId}", postId);

                // به‌روزرسانی وضعیت پست در صورت خطا
                try
                {
                    var post = await _postRepository.GetByIdAsync(postId);
                    if (post != null)
                    {
                        post.Status = "Failed";
                        post.ErrorMessage = ex.Message;
                        await _postRepository.UpdateAsync(post);
                    }
                }
                catch (Exception updateEx)
                {
                    _logger.LogError(updateEx, "Error updating post status after failure: {PostId}", postId);
                }

                throw;
            }
        }

        private async Task<string> GetMediaPublicUrlAsync(MediaFile mediaFile)
        {
            // اگر فایل در Cloud Storage ذخیره شده باشد
            if (!string.IsNullOrEmpty(mediaFile.CloudUrl))
                return mediaFile.CloudUrl;

            // در غیر این صورت URL محلی
            return $"https://yourdomain.com/uploads/{mediaFile.FileName}";
        }

        private async Task<string> GetThumbnailPublicUrlAsync(MediaFile mediaFile)
        {
            if (!string.IsNullOrEmpty(mediaFile.ThumbnailUrl))
                return mediaFile.ThumbnailUrl;

            if (!string.IsNullOrEmpty(mediaFile.ThumbnailPath))
            {
                var thumbnailFileName = Path.GetFileName(mediaFile.ThumbnailPath);
                return $"https://yourdomain.com/uploads/thumbnails/{thumbnailFileName}";
            }

            return null;
        }

        private async Task<ScheduledPostDto> MapToScheduledPostDto(Post post)
        {
            var account = await _accountRepository.GetByIdAsync(post.AccountId);

            return new ScheduledPostDto
            {
                Id = post.Id,
                AccountId = post.AccountId,
                AccountUsername = account?.InstagramUsername,
                MediaFileId = 0, // باید از رابطه با MediaFile دریافت شود
                MediaUrl = post.MediaUrl,
                ThumbnailUrl = post.ThumbnailUrl,
                MediaType = post.MediaType,
                Caption = post.Caption,
                ScheduledDate = post.ScheduledDate,
                Status = post.Status,
                IsStory = post.IsStory,
                StoryLink = post.StoryLink,
                CreatedDate = post.CreatedDate,
                HangfireJobId = post.HangfireJobId
            };
        }
    }
}

