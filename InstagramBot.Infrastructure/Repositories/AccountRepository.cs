using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using InstagramBot.Core.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace InstagramBot.Infrastructure.Repositories
{
    public class AccountRepository : IAccountRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly IEncryptionService _encryptionService;

        public AccountRepository(ApplicationDbContext context, IEncryptionService encryptionService)
        {
            _context = context;
            _encryptionService = encryptionService;
        }

        public async Task<Account> GetByIdAsync(int id)
        {
            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (account != null)
            {
                // رمزگشایی توکن‌ها هنگام خواندن از دیتابیس
                if (!string.IsNullOrEmpty(account.AccessToken))
                {
                    account.AccessToken = _encryptionService.Decrypt(account.AccessToken);
                }

                if (!string.IsNullOrEmpty(account.PageAccessToken))
                {
                    account.PageAccessToken = _encryptionService.Decrypt(account.PageAccessToken);
                }
            }

            return account;
        }

        public async Task<Account> CreateAsync(Account account)
        {
            // رمزنگاری توکن‌ها قبل از ذخیره در دیتابیس
            if (!string.IsNullOrEmpty(account.AccessToken))
            {
                account.AccessToken = _encryptionService.Encrypt(account.AccessToken);
            }

            if (!string.IsNullOrEmpty(account.PageAccessToken))
            {
                account.PageAccessToken = _encryptionService.Encrypt(account.PageAccessToken);
            }

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            // رمزگشایی برای بازگرداندن به کلاینت
            if (!string.IsNullOrEmpty(account.AccessToken))
            {
                account.AccessToken = _encryptionService.Decrypt(account.AccessToken);
            }

            if (!string.IsNullOrEmpty(account.PageAccessToken))
            {
                account.PageAccessToken = _encryptionService.Decrypt(account.PageAccessToken);
            }

            return account;
        }

        public async Task<Account> UpdateAsync(Account account)
        {
            var existingAccount = await _context.Accounts.FindAsync(account.Id);
            if (existingAccount == null)
                return null;

            // به‌روزرسانی فیلدها
            existingAccount.InstagramUsername = account.InstagramUsername;
            existingAccount.ExpiresIn = account.ExpiresIn;
            existingAccount.LastRefreshed = account.LastRefreshed;
            existingAccount.IsActive = account.IsActive;

            // رمزنگاری توکن‌های جدید
            if (!string.IsNullOrEmpty(account.AccessToken))
            {
                existingAccount.AccessToken = _encryptionService.Encrypt(account.AccessToken);
            }

            if (!string.IsNullOrEmpty(account.PageAccessToken))
            {
                existingAccount.PageAccessToken = _encryptionService.Encrypt(account.PageAccessToken);
            }

            await _context.SaveChangesAsync();

            return await GetByIdAsync(account.Id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var account = await _context.Accounts.FindAsync(id);
            if (account == null)
                return false;

            _context.Accounts.Remove(account);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Account>> GetByUserIdAsync(int userId)
        {
            var accounts = await _context.Accounts
                .Where(a => a.UserId == userId)
                .Include(a => a.User)
                .ToListAsync();

            // رمزگشایی توکن‌ها برای همه حساب‌ها
            foreach (var account in accounts)
            {
                if (!string.IsNullOrEmpty(account.AccessToken))
                {
                    account.AccessToken = _encryptionService.Decrypt(account.AccessToken);
                }

                if (!string.IsNullOrEmpty(account.PageAccessToken))
                {
                    account.PageAccessToken = _encryptionService.Decrypt(account.PageAccessToken);
                }
            }

            return accounts;
        }

        public async Task<Account> GetByInstagramUserIdAsync(string instagramUserId)
        {
            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.InstagramUserId == instagramUserId);

            if (account != null)
            {
                // رمزگشایی توکن‌ها هنگام خواندن از دیتابیس
                if (!string.IsNullOrEmpty(account.AccessToken))
                {
                    account.AccessToken = _encryptionService.Decrypt(account.AccessToken);
                }

                if (!string.IsNullOrEmpty(account.PageAccessToken))
                {
                    account.PageAccessToken = _encryptionService.Decrypt(account.PageAccessToken);
                }
            }

            return account;
        }
    }
}
