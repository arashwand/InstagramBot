using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Entities
{
    public class AutoReplyRule
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public List<string> Keywords { get; set; } // stored as JSON
        public string ReplyMessage { get; set; }
        public bool IsActive { get; set; }
        public int Priority { get; set; }
        public MatchType MatchType { get; set; }
        public int MaxRepliesPerHour { get; set; }
        public int DelayMinutes { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
