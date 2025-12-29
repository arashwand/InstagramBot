using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface IAutoReplyService
    {
        Task ProcessAutoReplyAsync(int accountId, int interactionId);
    }
}
