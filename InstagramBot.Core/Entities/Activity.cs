using System;

namespace InstagramBot.Core.Entities
{
    public class Activity
    {
        public int Id { get; set; }
        public string Type { get; set; } = ""; // مثلاً "Post", "Comment", "Error"
        public string Message { get; set; } = ""; // توضیح فعالیت
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? AccountId { get; set; } // اختیاری، برای مرتبط کردن با حساب
        public Account? Account { get; set; } // رابطه با Account
    }
}