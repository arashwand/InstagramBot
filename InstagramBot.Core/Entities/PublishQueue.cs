using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Entities
{
    public class PublishQueue
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int PostId { get; set; }
        public string QueueType { get; set; } // Post, Story, Comment, Like
        public string Priority { get; set; } // High, Normal, Low
        public string Status { get; set; } // Pending, Processing, Completed, Failed
        public DateTime ScheduledTime { get; set; }
        public DateTime? ProcessedTime { get; set; }
        public int AttemptCount { get; set; }
        public string ErrorMessage { get; set; }
        public string JobData { get; set; } // JSON data for the job
        public DateTime CreatedDate { get; set; }

        public Account Account { get; set; }
        public Post Post { get; set; }
    }

    public class RateLimitTracker
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string ActionType { get; set; }
        public DateTime ActionTime { get; set; }
        public string IpAddress { get; set; }

        public Account Account { get; set; }
    }
}

