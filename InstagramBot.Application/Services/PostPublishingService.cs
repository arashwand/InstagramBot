using InstagramBot.Application.DTOs;
using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services
{
    public class PostPublishingService : IPostPublishingService
    {
        private readonly IInstagramGraphApiClient _apiClient;
        private readonly IAccountRepository _accountRepository;
        private readonly IMediaFileRepository _mediaRepository;
        private readonly IPostRepository _postRepository;
        private readonly ICustomLogService _logService;
        private readonly ILogger<PostPublishingService> _logger;

        public PostPublishingService(
            IInstagramGraphApiClient apiClient,
            IAccountRepository accountRepository,
            IMediaFileRepository mediaRepository,
            IPostRepository postRepository,
            ICustomLogService logService,
            ILogger<PostPublishingService> logger)
        {
            _apiClient = apiClient;
            _accountRepository = accountRepository;
            _mediaRepository = mediaRepository;
            _postRepository = postRepository;
            _logService = logService;
            _logger = logger;
        }

        public async Task<PublishResult> PublishPostAsync(Post post, Account account)
        {
            try
            {
                _logger.LogInformation("Publishing post {PostId} for account {AccountId}", post.Id, account.Id);

                // اعتبارسنجی محتوا
                if (!await ValidatePostContentAsync(post.Caption, post.MediaUrl))
                {
                    return new PublishResult
                    {
                        Success = false,
                        ErrorMessage = "محتوای پست نامعتبر است."
                    };
                }

                if (post.IsStory)
                {
                    return await PublishStoryAsync(post, account);
                }
                else
                {
                    return await PublishRegularPostAsync(post, account);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing post {PostId}", post.Id);
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        private async Task<PublishResult> PublishRegularPostAsync(Post post, Account account)
        {
            try
            {
                // مرحله 1: ایجاد رسانه
                var createMediaDto = new CreateMediaDto
                {
                    Caption = post.Caption
                };

                if (post.MediaType == "Image")
                {
                    createMediaDto.ImageUrl = post.MediaUrl;
                }
                else if (post.MediaType == "Video")
                {
                    createMediaDto.VideoUrl = post.MediaUrl;
                }

                var creationId = await _apiClient.CreateMediaAsync(
                    account.InstagramUserId,
                    createMediaDto,
                    account.AccessToken);

                if (string.IsNullOrEmpty(creationId))
                {
                    return new PublishResult
                    {
                        Success = false,
                        ErrorMessage = "خطا در ایجاد رسانه در اینستاگرام."
                    };
                }

                // مرحله 2: انتشار رسانه
                var publishedMedia = await _apiClient.PublishMediaAsync(
                    account.InstagramUserId,
                    creationId,
                    account.AccessToken);

                if (publishedMedia == null)
                {
                    return new PublishResult
                    {
                        Success = false,
                        ErrorMessage = "خطا در انتشار رسانه در اینستاگرام."
                    };
                }

                await _logService.LogInstagramApiCallAsync(account.Id, "PublishPost", true);

                return new PublishResult
                {
                    Success = true,
                    InstagramMediaId = publishedMedia.Id,
                    Permalink = publishedMedia.Permalink,
                    PublishedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await _logService.LogInstagramApiCallAsync(account.Id, "PublishPost", false, ex.Message);
                throw;
            }
        }

        public async Task<PublishResult> PublishStoryAsync(Post post, Account account)
        {
            try
            {
                var storyDto = new InstagramStoryDto
                {
                    MediaType = post.MediaType,
                    MediaUrl = post.MediaUrl,
                    Link = post.StoryLink
                };

                var storyId = await _apiClient.CreateStoryAsync(
                    account.InstagramUserId,
                    storyDto,
                    account.AccessToken);

                if (string.IsNullOrEmpty(storyId))
                {
                    return new PublishResult
                    {
                        Success = false,
                        ErrorMessage = "خطا در انتشار استوری."
                    };
                }

                await _logService.LogInstagramApiCallAsync(account.Id, "PublishStory", true);

                return new PublishResult
                {
                    Success = true,
                    InstagramMediaId = storyId,
                    PublishedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                await _logService.LogInstagramApiCallAsync(account.Id, "PublishStory", false, ex.Message);
                throw;
            }
        }

        public async Task<PublishResult> PublishPostNowAsync(PostPublishDto publishDto, int userId)
        {
            try
            {
                // اعتبارسنجی حساب
                var account = await _accountRepository.GetByIdAsync(publishDto.AccountId);
                if (account == null || account.UserId != userId)
                {
                    return new PublishResult
                    {
                        Success = false,
                        ErrorMessage = "حساب یافت نشد یا شما مجاز به استفاده از آن نیستید."
                    };
                }

                // اعتبارسنجی فایل رسانه
                var mediaFile = await _mediaRepository.GetByIdAsync(publishDto.MediaFileId);
                if (mediaFile == null || mediaFile.UserId != userId)
                {
                    return new PublishResult
                    {
                        Success = false,
                        ErrorMessage = "فایل رسانه یافت نشد."
                    };
                }

                // ایجاد پست موقت برای انتشار
                var post = new Post
                {
                    AccountId = publishDto.AccountId,
                    MediaType = mediaFile.MediaType,
                    Caption = publishDto.Caption,
                    MediaUrl = await GetMediaPublicUrlAsync(mediaFile),
                    IsStory = publishDto.IsStory,
                    StoryLink = publishDto.StoryLink,
                    Status = "Publishing",
                    CreatedDate = DateTime.UtcNow
                };

                // ذخیره پست
                var savedPost = await _postRepository.CreateAsync(post);

                // انتشار پست
                var result = await PublishPostAsync(savedPost, account);

                // به‌روزرسانی وضعیت پست
                savedPost.Status = result.Success ? "Published" : "Failed";
                savedPost.PublishedDate = result.Success ? DateTime.UtcNow : null;
                savedPost.InstagramMediaId = result.InstagramMediaId;
                savedPost.ErrorMessage = result.Success ? null : result.ErrorMessage;

                await _postRepository.UpdateAsync(savedPost);

                if (result.Success)
                {
                    await _logService.LogUserActivityAsync(userId, "PostPublished",
                        $"Post published immediately on account {account.InstagramUsername}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing post immediately for user {UserId}", userId);
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = "خطا در انتشار پست."
                };
            }
        }

        public async Task<PublishResult> PublishCarouselPostAsync(CarouselPostDto carouselDto, int userId)
        {
            try
            {
                // اعتبارسنجی حساب
                var account = await _accountRepository.GetByIdAsync(carouselDto.AccountId);
                if (account == null || account.UserId != userId)
                {
                    return new PublishResult
                    {
                        Success = false,
                        ErrorMessage = "حساب یافت نشد."
                    };
                }

                if (carouselDto.MediaFileIds == null || carouselDto.MediaFileIds.Count < 2 || carouselDto.MediaFileIds.Count > 10)
                {
                    return new PublishResult
                    {
                        Success = false,
                        ErrorMessage = "کاروسل باید شامل 2 تا 10 رسانه باشد."
                    };
                }

                var mediaFiles = new List<MediaFile>();
                foreach (var mediaId in carouselDto.MediaFileIds)
                {
                    var mediaFile = await _mediaRepository.GetByIdAsync(mediaId);
                    if (mediaFile == null || mediaFile.UserId != userId)
                    {
                        return new PublishResult
                        {
                            Success = false,
                            ErrorMessage = $"فایل رسانه {mediaId} یافت نشد."
                        };
                    }
                    mediaFiles.Add(mediaFile);
                }

                // ایجاد آیتم‌های کاروسل
                var carouselItems = new List<string>();
                foreach (var mediaFile in mediaFiles)
                {
                    var createMediaDto = new CreateMediaDto
                    {
                        IsCarouselItem = true
                    };

                    if (mediaFile.MediaType == "Image")
                    {
                        createMediaDto.ImageUrl = await GetMediaPublicUrlAsync(mediaFile);
                    }
                    else if (mediaFile.MediaType == "Video")
                    {
                        createMediaDto.VideoUrl = await GetMediaPublicUrlAsync(mediaFile);
                    }

                    var itemId = await _apiClient.CreateMediaAsync(
                        account.InstagramUserId,
                        createMediaDto,
                        account.AccessToken);

                    if (string.IsNullOrEmpty(itemId))
                    {
                        return new PublishResult
                        {
                            Success = false,
                            ErrorMessage = "خطا در ایجاد آیتم کاروسل."
                        };
                    }

                    carouselItems.Add(itemId);
                }

                // ایجاد کاروسل اصلی
                var carouselCreateDto = new CreateMediaDto
                {
                    Caption = carouselDto.Caption,
                    // CarouselItems = carouselItems // باید در پیاده‌سازی API Client اضافه شود
                };

                var carouselId = await _apiClient.CreateMediaAsync(
                    account.InstagramUserId,
                    carouselCreateDto,
                    account.AccessToken);

                // انتشار کاروسل
                var publishedCarousel = await _apiClient.PublishMediaAsync(
                    account.InstagramUserId,
                    carouselId,
                    account.AccessToken);

                if (publishedCarousel == null)
                {
                    return new PublishResult
                    {
                        Success = false,
                        ErrorMessage = "خطا در انتشار کاروسل."
                    };
                }

                // ذخیره پست کاروسل
                var post = new Post
                {
                    AccountId = carouselDto.AccountId,
                    MediaType = "Carousel",
                    Caption = carouselDto.Caption,
                    Status = "Published",
                    PublishedDate = DateTime.UtcNow,
                    InstagramMediaId = publishedCarousel.Id,
                    CreatedDate = DateTime.UtcNow
                };

                await _postRepository.CreateAsync(post);

                await _logService.LogUserActivityAsync(userId, "CarouselPublished",
                    $"Carousel post published on account {account.InstagramUsername}");

                return new PublishResult
                {
                    Success = true,
                    InstagramMediaId = publishedCarousel.Id,
                    Permalink = publishedCarousel.Permalink,
                    PublishedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing carousel post for user {UserId}", userId);
                return new PublishResult
                {
                    Success = false,
                    ErrorMessage = "خطا در انتشار کاروسل."
                };
            }
        }

        public async Task<bool> ValidatePostContentAsync(string caption, string mediaUrl)
        {
            try
            {
                // بررسی طول کپشن
                if (!string.IsNullOrEmpty(caption) && caption.Length > 2200)
                {
                    return false;
                }

                // بررسی وجود URL رسانه
                if (string.IsNullOrEmpty(mediaUrl))
                {
                    return false;
                }

                // بررسی دسترسی به فایل رسانه
                using var httpClient = new HttpClient();
                var response = await httpClient.HeadAsync(mediaUrl);

                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error validating post content");
                return false;
            }
        }

        private async Task<string> GetMediaPublicUrlAsync(MediaFile mediaFile)
        {
            if (!string.IsNullOrEmpty(mediaFile.CloudUrl))
                return mediaFile.CloudUrl;

            return $"https://yourdomain.com/uploads/{mediaFile.FileName}";
        }
    }
}
