using System;
using System.Collections.Generic;

namespace InstagramBot.DTOs
{
    public class CreatePostDto
    {
        public string Caption { get; set; } = "";
        public List<string> MediaUrls { get; set; } = new();
        public string InstagramMediaId { get; set; } = "";
        public bool IsStory { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public int AccountId { get; set; }
    }

    public class PostDto
    {
        public int Id { get; set; }
        public string Caption { get; set; } = "";
        public List<string> MediaUrls { get; set; } = new();
        public string InstagramMediaId { get; set; } = "";
        public bool IsStory { get; set; }
        public DateTime? ScheduledAt { get; set; }
        public DateTime? PublishedAt { get; set; }
        public string Status { get; set; } = "";
        public int AccountId { get; set; }
    }
}