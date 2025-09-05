using FFMpegCore;
using InstagramBot.Application.DTOs;
using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;

namespace InstagramBot.Application.Services
{
    public class MediaFileService : IMediaFileService
    {
        private readonly IMediaFileRepository _mediaRepository;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MediaFileService> _logger;
        private readonly IWebHostEnvironment _environment;

        private readonly string _uploadsPath;
        private readonly long _maxFileSize;
        private readonly string[] _allowedImageTypes;
        private readonly string[] _allowedVideoTypes;

        public MediaFileService(
            IMediaFileRepository mediaRepository,
            IConfiguration configuration,
            ILogger<MediaFileService> logger,
            IWebHostEnvironment environment)
        {
            _mediaRepository = mediaRepository;
            _configuration = configuration;
            _logger = logger;
            _environment = environment;

            _uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            _maxFileSize = _configuration.GetValue<long>("MediaStorage:MaxFileSize");
            _allowedImageTypes = _configuration.GetSection("MediaStorage:AllowedImageTypes").Get<string[]>();
            _allowedVideoTypes = _configuration.GetSection("MediaStorage:AllowedVideoTypes").Get<string[]>();

            // ایجاد پوشه uploads در صورت عدم وجود
            Directory.CreateDirectory(_uploadsPath);
        }

        public async Task<MediaProcessingResult> UploadMediaAsync(MediaUploadDto uploadDto, int userId)
        {
            var result = new MediaProcessingResult
            {
                Errors = new List<string>()
            };

            try
            {
                // اعتبارسنجی فایل
                if (!await ValidateMediaFileAsync(uploadDto.File))
                {
                    result.Errors.Add("فایل نامعتبر است.");
                    return result;
                }

                // تشخیص نوع رسانه
                var fileExtension = Path.GetExtension(uploadDto.File.FileName).ToLower();
                var isImage = _allowedImageTypes.Contains(fileExtension);
                var isVideo = _allowedVideoTypes.Contains(fileExtension);

                if (!isImage && !isVideo)
                {
                    result.Errors.Add("فرمت فایل پشتیبانی نمی‌شود.");
                    return result;
                }

                // تولید نام یکتا برای فایل
                var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(_uploadsPath, uniqueFileName);

                // ذخیره فایل اصلی
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await uploadDto.File.CopyToAsync(stream);
                }

                var mediaFile = new MediaFile
                {
                    FileName = uniqueFileName,
                    OriginalFileName = uploadDto.File.FileName,
                    MediaType = isImage ? "Image" : "Video",
                    MimeType = uploadDto.File.ContentType,
                    FileSize = uploadDto.File.Length,
                    LocalPath = filePath,
                    UploadedAt = DateTime.UtcNow,
                    UserId = userId,
                    IsDeleted = false
                };

                // پردازش بر اساس نوع فایل
                if (isImage)
                {
                    await ProcessImageAsync(mediaFile, filePath);
                }
                else if (isVideo)
                {
                    await ProcessVideoAsync(mediaFile, filePath);
                }

                // ذخیره در دیتابیس
                var savedMedia = await _mediaRepository.CreateAsync(mediaFile);

                result.Success = true;
                result.MediaFile = MapToDto(savedMedia);
                result.Message = "فایل با موفقیت آپلود شد.";

                _logger.LogInformation("Media file uploaded successfully: {FileName} for user {UserId}",
                    uniqueFileName, userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading media file for user {UserId}", userId);
                result.Errors.Add("خطا در آپلود فایل.");
                return result;
            }
        }

        private async Task ProcessImageAsync(MediaFile mediaFile, string filePath)
        {
            try
            {
                using var image = await Image.LoadAsync(filePath);

                // ثبت ابعاد تصویر
                mediaFile.Width = image.Width;
                mediaFile.Height = image.Height;

                // تولید تصویر بندانگشتی
                var thumbnailFileName = $"thumb_{mediaFile.FileName}";
                var thumbnailPath = Path.Combine(_uploadsPath, "thumbnails");
                Directory.CreateDirectory(thumbnailPath);

                var fullThumbnailPath = Path.Combine(thumbnailPath, thumbnailFileName);

                var thumbnailSize = _configuration.GetSection("MediaStorage:ThumbnailSize");
                var thumbWidth = thumbnailSize.GetValue<int>("Width");
                var thumbHeight = thumbnailSize.GetValue<int>("Height");

                using var thumbnail = image.Clone(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(thumbWidth, thumbHeight),
                    Mode = ResizeMode.Crop
                }));

                await thumbnail.SaveAsJpegAsync(fullThumbnailPath, new JpegEncoder { Quality = 85 });

                mediaFile.ThumbnailPath = fullThumbnailPath;

                _logger.LogInformation("Image processed successfully: {FileName}", mediaFile.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image: {FileName}", mediaFile.FileName);
                throw;
            }
        }

        private async Task ProcessVideoAsync(MediaFile mediaFile, string filePath)
        {
            try
            {
                var mediaInfo = await FFProbe.AnalyseAsync(filePath);

                // ثبت اطلاعات ویدیو
                mediaFile.Width = mediaInfo.PrimaryVideoStream?.Width ?? 0;
                mediaFile.Height = mediaInfo.PrimaryVideoStream?.Height ?? 0;
                mediaFile.Duration = (int?)mediaInfo.Duration.TotalSeconds;

                // تولید تصویر بندانگشتی از ویدیو
                var thumbnailFileName = $"thumb_{Path.GetFileNameWithoutExtension(mediaFile.FileName)}.jpg";
                var thumbnailPath = Path.Combine(_uploadsPath, "thumbnails");
                Directory.CreateDirectory(thumbnailPath);

                var fullThumbnailPath = Path.Combine(thumbnailPath, thumbnailFileName);

                await FFMpeg.SnapshotAsync(filePath, fullThumbnailPath,null, TimeSpan.FromSeconds(1));

                mediaFile.ThumbnailPath = fullThumbnailPath;

                _logger.LogInformation("Video processed successfully: {FileName}", mediaFile.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing video: {FileName}", mediaFile.FileName);
                throw;
            }
        }

        public async Task<bool> ValidateMediaFileAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return false;

            if (file.Length > _maxFileSize)
                return false;

            var extension = Path.GetExtension(file.FileName).ToLower();
            var allowedExtensions = _allowedImageTypes.Concat(_allowedVideoTypes).ToArray();

            return allowedExtensions.Contains(extension);
        }

        public async Task<MediaFileDto> GetMediaByIdAsync(int mediaId, int userId)
        {
            var media = await _mediaRepository.GetByIdAsync(mediaId);

            if (media == null || media.UserId != userId || media.IsDeleted)
                return null;

            return MapToDto(media);
        }

        public async Task<List<MediaFileDto>> GetUserMediaAsync(int userId, int page = 1, int pageSize = 20)
        {
            var mediaFiles = await _mediaRepository.GetByUserIdAsync(userId, page, pageSize);
            return mediaFiles.Select(MapToDto).ToList();
        }

        public async Task<bool> DeleteMediaAsync(int mediaId, int userId)
        {
            try
            {
                var media = await _mediaRepository.GetByIdAsync(mediaId);

                if (media == null || media.UserId != userId)
                    return false;

                // حذف نرم (Soft Delete)
                media.IsDeleted = true;
                await _mediaRepository.UpdateAsync(media);

                // حذف فایل‌های فیزیکی (اختیاری - می‌توان در یک Job جداگانه انجام داد)
                try
                {
                    if (File.Exists(media.LocalPath))
                        File.Delete(media.LocalPath);

                    if (!string.IsNullOrEmpty(media.ThumbnailPath) && File.Exists(media.ThumbnailPath))
                        File.Delete(media.ThumbnailPath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete physical files for media {MediaId}", mediaId);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting media {MediaId} for user {UserId}", mediaId, userId);
                return false;
            }
        }

        public async Task<string> GetMediaUrlAsync(int mediaId)
        {
            var media = await _mediaRepository.GetByIdAsync(mediaId);
            if (media == null || media.IsDeleted)
                return null;

            // اگر فایل در Cloud Storage ذخیره شده باشد
            if (!string.IsNullOrEmpty(media.CloudUrl))
                return media.CloudUrl;

            // در غیر این صورت URL محلی
            return $"/uploads/{media.FileName}";
        }

        public async Task<string> GetThumbnailUrlAsync(int mediaId)
        {
            var media = await _mediaRepository.GetByIdAsync(mediaId);
            if (media == null || media.IsDeleted || string.IsNullOrEmpty(media.ThumbnailPath))
                return null;

            // اگر تصویر بندانگشتی در Cloud Storage ذخیره شده باشد
            if (!string.IsNullOrEmpty(media.ThumbnailUrl))
                return media.ThumbnailUrl;

            // در غیر این صورت URL محلی
            var thumbnailFileName = Path.GetFileName(media.ThumbnailPath);
            return $"/uploads/thumbnails/{thumbnailFileName}";
        }

        private MediaFileDto MapToDto(MediaFile media)
        {
            return new MediaFileDto
            {
                Id = media.Id.ToString(),
                FileName = media.FileName,
                OriginalFileName = media.OriginalFileName,
                MediaType = media.MediaType,
                MimeType = media.MimeType,
                FileSize = media.FileSize,
                LocalPath = media.LocalPath,
                CloudUrl = media.CloudUrl,
                ThumbnailPath = media.ThumbnailPath,
                ThumbnailUrl = media.ThumbnailUrl,
                Width = media.Width,
                Height = media.Height,
                Duration = media.Duration,
                UploadedAt = media.UploadedAt,
                UserId = media.UserId
            };
        }
    }
}

