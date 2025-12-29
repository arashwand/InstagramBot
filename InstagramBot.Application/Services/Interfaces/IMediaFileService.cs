using InstagramBot.DTOs;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IMediaFileService
    {
        Task<MediaProcessingResult> UploadMediaAsync(MediaUploadDto uploadDto, int userId);
        Task<MediaFileDto> GetMediaByIdAsync(int mediaId, int userId);
        Task<List<MediaFileDto>> GetUserMediaAsync(int userId, int page = 1, int pageSize = 20);
        Task<bool> DeleteMediaAsync(int mediaId, int userId);
        Task<string> GetMediaUrlAsync(int mediaId);
        Task<string> GetThumbnailUrlAsync(int mediaId);
        Task<bool> ValidateMediaFileAsync(IFormFile file);
    }
}
