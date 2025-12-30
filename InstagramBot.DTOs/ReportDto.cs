using System.Collections.Generic;

namespace InstagramBot.DTOs
{
    public class AnalyticsReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalPosts { get; set; }
        public int TotalStories { get; set; }
        public int TotalImpressions { get; set; }
        public int TotalReach { get; set; }
        public int TotalLikes { get; set; }
        public int TotalComments { get; set; }
        public double AverageEngagementRate { get; set; }
        public int FollowersGrowth { get; set; }
        public List<TopPostDto> TopPosts { get; set; } = new();
        public Dictionary<string, int> PostsByDay { get; set; } = new();
        public Dictionary<string, double> EngagementByHour { get; set; } = new();
    }
}