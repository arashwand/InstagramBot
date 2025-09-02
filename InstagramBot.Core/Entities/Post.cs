using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Entities
{
    public class Post
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public string MediaType { get; set; }
        public string Caption { get; set; }
        public string MediaUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public DateTime ScheduledDate { get; set; }
        public DateTime? PublishedDate { get; set; }
        public string Status { get; set; }
        public string InstagramMediaId { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsStory { get; set; }
        public string StoryLink { get; set; }
        public DateTime CreatedDate { get; set; }

        public Account Account { get; set; } // Navigation property
        public ICollection<Interaction> Interactions { get; set; } // Navigation property
        public ICollection<Analytic> Analytics { get; set; } // Navigation property

    }
}
