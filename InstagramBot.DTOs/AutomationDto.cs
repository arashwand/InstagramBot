using System.ComponentModel.DataAnnotations;

namespace InstagramBot.DTOs
{
    public class AutomationRuleDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "کلمات کلیدی الزامی است")]
        public List<string> Keywords { get; set; } = new List<string>();

        [Required(ErrorMessage = "پاسخ الزامی است")]
        public string Response { get; set; } = "";

        public bool IsActive { get; set; } = true;
        public int AccountId { get; set; }
        public string Name { get; set; }
        public string ReplyMessage { get; set; }
        public int Priority { get; set; }
        public int MaxRepliesPerHour { get; set; }
        public MatchType MatchType { get; set; }
        public int DelayMinutes { get; set; }
    }

    public enum MatchType
    {
        ContainsAny,
        ContainsAll,
        ExactMatch
    }
}