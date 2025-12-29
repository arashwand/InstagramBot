using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.DTOs
{
    public class AccountAnalyticsDto
    {
        public int AccountId { get; set; }
        public string AccountUsername { get; set; }
        public DateTime Date { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public int MediaCount { get; set; }
        public int Impressions { get; set; }
        public int Reach { get; set; }
        public int ProfileViews { get; set; }
        public int WebsiteClicks { get; set; }
        public double EngagementRate { get; set; }
    }

    public class PostAnalyticsDto
    {
        public int PostId { get; set; }
        public string InstagramMediaId { get; set; }
        public DateTime Date { get; set; }
        public int Impressions { get; set; }
        public int Reach { get; set; }
        public int LikesCount { get; set; }
        public int CommentsCount { get; set; }
        public int SavesCount { get; set; }
        public int SharesCount { get; set; }
        public int VideoViews { get; set; }
        public int ProfileVisits { get; set; }
        public double EngagementRate { get; set; }
        public Dictionary<string, int> AudienceGender { get; set; }
        public Dictionary<string, int> AudienceAge { get; set; }
        public Dictionary<string, int> AudienceCountry { get; set; }
    }

    public class StoryAnalyticsDto
    {
        public int PostId { get; set; }
        public string InstagramMediaId { get; set; }
        public DateTime Date { get; set; }
        public int Impressions { get; set; }
        public int Reach { get; set; }
        public int Replies { get; set; }
        public int TapsForward { get; set; }
        public int TapsBack { get; set; }
        public int Exits { get; set; }
        public int ProfileVisits { get; set; }
        public int WebsiteClicks { get; set; }
    }

    public class AnalyticsReportDto
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalPosts { get; set; }
        public int TotalStories { get; set; }
        public long TotalImpressions { get; set; }
        public long TotalReach { get; set; }
        public long TotalLikes { get; set; }
        public long TotalComments { get; set; }
        public double AverageEngagementRate { get; set; }
        public int FollowersGrowth { get; set; }
        public List<PostAnalyticsDto> TopPosts { get; set; }
        public Dictionary<string, int> PostsByDay { get; set; }
        public Dictionary<string, double> EngagementByHour { get; set; }
    }
}
