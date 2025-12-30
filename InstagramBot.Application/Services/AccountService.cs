using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Interfaces;
using InstagramBot.DTOs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IAccountRepository _accountRepository;
        private readonly ILogger<AccountService> _logger;

        public AccountService(IAccountRepository accountRepository, ILogger<AccountService> logger)
        {
            _accountRepository = accountRepository;
            _logger = logger;
        }

        public async Task<List<AccountDto>> GetAllAccountsAsync()
        {
            var accounts = await _accountRepository.GetByUserIdAsync(0); // Assuming user context
            return accounts.Select(a => new AccountDto
            {
                Id = a.Id,
                InstagramUsername = a.InstagramUsername,
                IsActive = a.IsActive,
                LastRefreshed = a.LastRefreshed
            }).ToList();
        }

        public async Task<AccountDto> GetAccountByIdAsync(int id)
        {
            var account = await _accountRepository.GetByIdAsync(id);
            return new AccountDto
            {
                Id = account.Id,
                InstagramUsername = account.InstagramUsername,
                IsActive = account.IsActive,
                LastRefreshed = account.LastRefreshed
            };
        }

        public async Task CreateAccountAsync(CreateAccountDto dto)
        {
            var account = new Core.Entities.Account
            {
                InstagramUsername = dto.InstagramUsername,
                AccessToken = dto.AccessToken,
                PageAccessToken = dto.PageAccessToken,
                ExpiresIn = dto.ExpiresIn,
                IsActive = true
            };
            await _accountRepository.CreateAsync(account);
        }

        public async Task UpdateAccountAsync(int id, AccountDto dto)
        {
            var account = await _accountRepository.GetByIdAsync(id);
            account.InstagramUsername = dto.InstagramUsername;
            account.IsActive = dto.IsActive;
            await _accountRepository.UpdateAsync(account);
        }

        public async Task DeleteAccountAsync(int id)
        {
            await _accountRepository.DeleteAsync(id);
        }
    }
}