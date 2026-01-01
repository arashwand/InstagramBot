using InstagramBot.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Interfaces
{
    public interface IAccountRepository
    {
        Task<Account> GetByIdAsync(int accountId, int userId);
        Task<Account> CreateAsync(Account account);
        Task<Account> UpdateAsync(Account account);
        Task<bool> DeleteAsync(int id);
        Task<List<Account>> GetByUserIdAsync(int userId);
        Task<Account> GetByInstagramUserIdAsync(string instagramUserId);
        Task<List<Account>> GetAllActiveAsync();
    }
}
