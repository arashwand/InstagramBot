using InstagramBot.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IAccountService
    {
        Task<List<AccountDto>> GetAllAccountsAsync();
        Task<AccountDto> GetAccountByIdAsync(int id);
        Task CreateAccountAsync(CreateAccountDto account);
        Task UpdateAccountAsync(int id, AccountDto account);
        Task DeleteAccountAsync(int id);
    }
}