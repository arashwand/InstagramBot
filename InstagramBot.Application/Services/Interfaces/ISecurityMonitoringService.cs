using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services.Interfaces
{
    public interface ISecurityMonitoringService
    {
        Task MonitorFailedLoginAttemptsAsync(string username, string ipAddress);
        Task MonitorSuspiciousActivityAsync(int userId, string activity, string ipAddress);
        Task CheckRateLimitAsync(int userId, string endpoint);
    }
}
