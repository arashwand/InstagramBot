using InstagramBot.Application.Services.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;

namespace InstagramBot.Infrastructure.Services
{
    public class EncryptionService : IEncryptionService
    {
        private readonly IDataProtector _protector;
        private readonly ILogger<EncryptionService> _logger;

        public EncryptionService(IDataProtectionProvider provider, ILogger<EncryptionService> logger)
        {
            _protector = provider.CreateProtector("InstagramBot.TokenProtection");
            _logger = logger;
        }

        public string Encrypt(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                return _protector.Protect(plainText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در رمزنگاری داده");
                throw;
            }
        }

        public string Decrypt(string cipherText)
        {
            if (string.IsNullOrEmpty(cipherText))
                return cipherText;

            try
            {
                return _protector.Unprotect(cipherText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در رمزگشایی داده");
                throw;
            }
        }

        public bool TryDecrypt(string cipherText, out string plainText)
        {
            plainText = null;

            if (string.IsNullOrEmpty(cipherText))
            {
                plainText = cipherText;
                return true;
            }

            try
            {
                plainText = _protector.Unprotect(cipherText);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "عدم موفقیت در رمزگشایی داده");
                return false;
            }
        }
    }
}
