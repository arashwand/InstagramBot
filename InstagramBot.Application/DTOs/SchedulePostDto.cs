using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.DTOs
{
    public class SchedulePostDto
    {
        public int AccountId { get; set; }
        public int MediaFileId { get; set; }
        public string Caption { get; set; }
        public DateTime ScheduledDate { get; set; }
        public bool IsStory { get; set; }
        public string StoryLink { get; set; }
        public List<string> UserTags { get; set; }
        public string LocationId { get; set; }
    }

    public class ScheduledPostDto
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string AccountUsername { get; set; }
        public int MediaFileId { get; set; }
        public string MediaUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string MediaType { get; set; }
        public string Caption { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string Status { get; set; }
        public bool IsStory { get; set; }
        public string StoryLink { get; set; }
        public DateTime CreatedDate { get; set; }
        public string HangfireJobId { get; set; }
    }

    public class UpdateScheduledPostDto
    {
        public string Caption { get; set; }
        public DateTime ScheduledDate { get; set; }
        public string StoryLink { get; set; }
        public List<string> UserTags { get; set; }
        public string LocationId { get; set; }
    }
}

