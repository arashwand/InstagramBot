using System;
using System.ComponentModel.DataAnnotations;

namespace InstagramBot.DTOs
{
    public class CreateAccountDto
    {
        [Required(ErrorMessage = "نام کاربری الزامی است")]
        public string InstagramUsername { get; set; } = "";

        [Required(ErrorMessage = "توکن دسترسی الزامی است")]
        public string AccessToken { get; set; } = "";

        public string PageAccessToken { get; set; } = "";

        [Range(1, int.MaxValue, ErrorMessage = "زمان انقضا باید مثبت باشد")]
        public int ExpiresIn { get; set; }
    }

    public class AccountDto
    {
        public int Id { get; set; }
        public string InstagramUsername { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime LastRefreshed { get; set; }
    }
}