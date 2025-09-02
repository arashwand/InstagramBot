using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Entities
{
    public class Analytic
    {
        public int Id { get; set; }
        public int AccountId { get; set; }
        public int? PostId { get; set; }
        public string MetricName { get; set; }
        public int MetricValue { get; set; }
        public DateTime DateRecorded { get; set; }
        public string Period { get; set; }

        public Account Account { get; set; } // Navigation property
        public Post Post { get; set; } // Navigation property

    }
}
