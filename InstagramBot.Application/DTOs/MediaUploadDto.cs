using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.DTOs
{
    public class MediaUploadDto
    {
        public IFormFile File { get; set; }
        public string Caption { get; set; }
        public string MediaType { get; set; } // Image, Video
        public List<string> Tags { get; set; }
        public string LocationId { get; set; }
    }

    public class MediaFileDto
    {
        public string Id { get; set; }
        public string FileName { get; set; }
        public string OriginalFileName { get; set; }
        public string MediaType { get; set; }
        public string MimeType { get; set; }
        public long FileSize { get; set; }
        public string LocalPath { get; set; }
        public string CloudUrl { get; set; }
        public string ThumbnailPath { get; set; }
        public string ThumbnailUrl { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int? Duration { get; set; } // برای ویدیو (به ثانیه)
        public DateTime UploadedAt { get; set; }
        public int UserId { get; set; }
    }

    public class MediaProcessingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public MediaFileDto MediaFile { get; set; }
        public List<string> Errors { get; set; }
    }

}
