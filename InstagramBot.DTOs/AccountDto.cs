using System;

namespace InstagramBot.DTOs
{
    public class CreateAccountDto
    {
        public string InstagramUsername { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public string PageAccessToken { get; set; } = "";
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