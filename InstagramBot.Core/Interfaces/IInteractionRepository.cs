using InstagramBot.Core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Core.Interfaces
{
    public interface IInteractionRepository
    {
        Task<Interaction> CreateAsync(Interaction interaction);
        Task<List<Interaction>> GetByAccountIdAsync(int accountId);
        Task<List<Interaction>> GetByPostIdAsync(int postId);
        Task<Interaction> UpdateAsync(Interaction interaction);
    }
}
