using InstagramBot.Core.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace InstagramBot.Core.Interfaces
{
    public interface IAutoReplyRepository
    {
        Task<AutoReplyRule> CreateAsync(AutoReplyRule rule);
        Task<List<AutoReplyRule>> GetByAccountIdAsync(int accountId);
        Task<List<AutoReplyRule>> GetActiveByAccountIdAsync(int accountId);
        Task<List<AutoReplyRule>> GetByUserIdAsync(int userId);  // اضافه شده
        Task<List<AutoReplyRule>> GetActiveByUserIdAsync(int userId);  // اضافه شده برای consistency
        Task<AutoReplyRule> GetByIdAsync(int id);
        Task<AutoReplyRule> UpdateAsync(AutoReplyRule rule);
        Task<bool> DeleteAsync(int id);
    }
}