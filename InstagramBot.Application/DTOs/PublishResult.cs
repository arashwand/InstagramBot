using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.DTOs
{
    public class PublishResult
    {
        public bool Success { get; set; }
        public string InstagramMediaId { get; set; }
        public string ErrorMessage { get; set; }
        public string Permalink { get; set; }
        public DateTime PublishedAt { get; set; }
    }

    public class PostPublishDto
    {
        public int AccountId { get; set; }
        public int MediaFileId { get; set; }
        public string Caption { get; set; }
        public bool IsStory { get; set; }
        public string StoryLink { get; set; }
        public List<string> UserTags { get; set; }
        public string LocationId { get; set; }
    }

    public class CarouselPostDto
    {
        public int AccountId { get; set; }
        public List<int> MediaFileIds { get; set; }
        public string Caption { get; set; }
        public List<string> UserTags { get; set; }
        public string LocationId { get; set; }
    }
}
