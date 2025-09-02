using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface ICustomLogService
    {
        Task LogUserActivityAsync(int userId, string activity, string details = null, string ipAddress = null);
        Task LogSecurityEventAsync(int? userId, string eventType, string description, string ipAddress = null);
        Task LogApiCallAsync(string endpoint, string method, int? userId, bool success, string errorMessage = null);
        Task LogInstagramApiCallAsync(int accountId, string endpoint, bool success, string errorMessage = null);
    }
}
