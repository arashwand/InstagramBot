using System;

namespace InstagramBot.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalAccounts { get; set; }
        public int TotalPosts { get; set; }
        public int TotalLikes { get; set; }
        public int TotalComments { get; set; }
        public double AccountsChange { get; set; }
        public double PostsChange { get; set; }
        public double LikesChange { get; set; }
        public double CommentsChange { get; set; }
    }

    public class TopPostDto
    {
        public int Id { get; set; }
        public string Caption { get; set; } = "";
        public string ThumbnailUrl { get; set; } = "";
        public string AccountName { get; set; } = "";
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public DateTime PublishedAt { get; set; }
    }


    public class ActivityDto
    {
        public int Id { get; set; }
        public string Type { get; set; } = "";
        public string Message { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }
}