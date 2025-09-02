using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Entities
{
    public class Account
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string InstagramUserId { get; set; }
        public string InstagramUsername { get; set; }
        public string AccessToken { get; set; } // Will be encrypted
        public int ExpiresIn { get; set; }
        public DateTime LastRefreshed { get; set; }
        public string PageAccessToken { get; set; } // Will be encrypted
        public string PageId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedDate { get; set; }

        public User User { get; set; } // Navigation property
        public ICollection<Post> Posts { get; set; } // Navigation property
        public ICollection<Interaction> Interactions { get; set; } // Navigation property
        public ICollection<Analytic> Analytics { get; set; } // Navigation property

    }
}
