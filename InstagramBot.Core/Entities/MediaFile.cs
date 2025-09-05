using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace InstagramBot.Core.Entities
{
    public class MediaFile
    {
        public int Id { get; set; }
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
        public int? Duration { get; set; }
        public DateTime UploadedAt { get; set; }
        public int UserId { get; set; }
        public bool IsDeleted { get; set; }

        public User User { get; set; }
        public ICollection<Post> Posts { get; set; }
    }
}
