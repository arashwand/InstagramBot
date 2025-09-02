using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Entities
{
    public class Interaction
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int? PostId { get; set; }
        public string InteractionType { get; set; }
        public string InstagramCommentId { get; set; }
        public string InstagramMessageId { get; set; }
        public string SenderUsername { get; set; }
        public string SenderId { get; set; }
        public string Content { get; set; }
        public string Sentiment { get; set; }
        public bool IsReplied { get; set; }
        public string ReplyContent { get; set; }
        public DateTime ReceivedDate { get; set; }

        public Account Account { get; set; } // Navigation property
        public Post Post { get; set; } // Navigation property

    }
}
