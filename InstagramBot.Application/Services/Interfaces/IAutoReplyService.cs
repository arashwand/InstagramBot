using InstagramBot.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IAutoReplyService
    {
        public interface IAutoReplyService
        {
            Task ProcessAutoReplyAsync(int accountId, int interactionId);
            Task<List<AutomationRuleDto>> GetAllRulesAsync(int userId);  // تغییر: اضافه کردن userId
            Task CreateRuleAsync(AutomationRuleDto rule, int userId);  // تغییر: اضافه کردن userId
        }
    }
}
