using System.ComponentModel.DataAnnotations;

namespace InstagramBot.DTOs
{
    public class AutomationRuleDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "کلمه کلیدی الزامی است")]
        public string Keyword { get; set; } = "";

        [Required(ErrorMessage = "پاسخ الزامی است")]
        public string Response { get; set; } = "";

        public bool IsActive { get; set; } = true;
        public int AccountId { get; set; }
    }
}