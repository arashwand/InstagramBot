namespace InstagramBot.Core.Entities
{
    public class AccountAnalytics
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public DateTime Date { get; set; }
        public int FollowersCount { get; set; }
        public int FollowingCount { get; set; }
        public int MediaCount { get; set; }
        public int Impressions { get; set; }
        public int Reach { get; set; }
        public int ProfileViews { get; set; }
        public int WebsiteClicks { get; set; }
        public double EngagementRate { get; set; }
        public DateTime CreatedDate { get; set; }

        public Account Account { get; set; }
    }

    public class PostAnalytics
    {
        public int Id { get; set; }
        public int PostId { get; set; }
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
        public string AudienceGender { get; set; }  // تغییر به string
        public string AudienceAge { get; set; }     // تغییر به string
        public string AudienceCountry { get; set; } // تغییر به string
        public DateTime CreatedDate { get; set; }

        public Post Post { get; set; }
        public string? InstagramMediaId { get; set; }
      
    }

    public class AnalyticsSnapshot
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public DateTime SnapshotDate { get; set; }
        public string DataType { get; set; } // Account, Post, Story
        public string RawData { get; set; } // JSON data from Instagram API
        public bool IsProcessed { get; set; }
        public DateTime CreatedDate { get; set; }

        public Account Account { get; set; }
    }
}
