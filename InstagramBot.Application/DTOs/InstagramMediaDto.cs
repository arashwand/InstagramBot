namespace InstagramBot.Application.DTOs
{
    public class InstagramMediaDto
    {
        public string Id { get; set; }
        public string MediaType { get; set; }
        public string MediaUrl { get; set; }
        public string ThumbnailUrl { get; set; }
        public string Caption { get; set; }
        public string Permalink { get; set; }
        public DateTime Timestamp { get; set; }
        public int LikeCount { get; set; }
        public int CommentsCount { get; set; }
    }

    public class InstagramCommentDto
    {
        public string Id { get; set; }
        public string Text { get; set; }
        public string Username { get; set; }
        public DateTime Timestamp { get; set; }
        public int LikeCount { get; set; }
        public List<InstagramCommentDto> Replies { get; set; }
    }

    public class InstagramInsightsDto
    {
        public string Name { get; set; }
        public string Period { get; set; }
        public List<InstagramInsightValue> Values { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
    }

    public class InstagramInsightValue
    {
        public int Value { get; set; }
        public DateTime? EndTime { get; set; }
    }

    public class CreateMediaDto
    {
        public string ImageUrl { get; set; }
        public string VideoUrl { get; set; }
        public string Caption { get; set; }
        public List<string> UserTags { get; set; }
        public string LocationId { get; set; }
        public bool IsCarouselItem { get; set; }
    }

    public class PublishMediaDto
    {
        public string CreationId { get; set; }
    }

    public class InstagramStoryDto
    {
        public string MediaType { get; set; }
        public string MediaUrl { get; set; }
        public string Link { get; set; }
        public List<string> UserTags { get; set; }
    }
}
