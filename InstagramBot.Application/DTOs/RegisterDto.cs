using System.ComponentModel.DataAnnotations;

namespace InstagramBot.Application.DTOs
{
    public class RegisterDto
    {
        [Required(ErrorMessage = "نام کاربری الزامی است.")]
        [StringLength(256, ErrorMessage = "نام کاربری نمی‌تواند بیش از 256 کاراکتر باشد.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "ایمیل الزامی است.")]
        [EmailAddress(ErrorMessage = "فرمت ایمیل نامعتبر است.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "رمز عبور الزامی است.")]
        [StringLength(100, ErrorMessage = "رمز عبور باید حداقل {2} و حداکثر {1} کاراکتر باشد.", MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "رمز عبور و تأیید رمز عبور مطابقت ندارند.")]
        public string ConfirmPassword { get; set; }
    }

    public class LoginDto
    {
        [Required(ErrorMessage = "نام کاربری الزامی است.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "رمز عبور الزامی است.")]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }

    public class RefreshTokenDto
    {
        [Required]
        public string Token { get; set; }
    }

    public class EnableTwoFactorDto
    {
        [Required(ErrorMessage = "کد تأیید الزامی است.")]
        [StringLength(6, ErrorMessage = "کد تأیید باید 6 رقم باشد.", MinimumLength = 6)]
        public string Code { get; set; }
    }

    public class VerifyTwoFactorDto
    {
        [Required]
        public int UserId { get; set; }

        [Required(ErrorMessage = "کد تأیید الزامی است.")]
        [StringLength(6, ErrorMessage = "کد تأیید باید 6 رقم باشد.", MinimumLength = 6)]
        public string Code { get; set; }
    }
}
