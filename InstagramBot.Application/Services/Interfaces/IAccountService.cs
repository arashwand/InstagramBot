using InstagramBot.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IAccountService
    {
        Task<List<AccountDto>> GetAllAccountsAsync();
        Task<AccountDto> GetAccountByIdAsync(int accountId,int userId);
        Task CreateAccountAsync(CreateAccountDto account);
        Task UpdateAccountAsync(int userId, AccountDto account);
        Task DeleteAccountAsync(int id);
    }
}