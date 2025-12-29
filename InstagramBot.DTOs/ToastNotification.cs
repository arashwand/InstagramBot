using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.DTOs
{
    public class ToastNotification
    {
        public Guid Id { get; set; }
        public string Message { get; set; } = "";
        public string Type { get; set; } = "";
        public DateTime Time { get; set; }
    }
}
