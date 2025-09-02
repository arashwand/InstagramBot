using InstagramBot.Application.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services
{
    public class SecurityMonitoringService : ISecurityMonitoringService
    {
        private readonly ICustomLogService _logService;
        private readonly IMemoryCache _cache;
        private readonly ILogger<SecurityMonitoringService> _logger;

        public SecurityMonitoringService(
            ICustomLogService logService,
            IMemoryCache cache,
            ILogger<SecurityMonitoringService> logger)
        {
            _logService = logService;
            _cache = cache;
            _logger = logger;
        }

        public async Task MonitorFailedLoginAttemptsAsync(string username, string ipAddress)
        {
            var key = $"failed_login_{ipAddress}";
            var attempts = _cache.Get<int>(key);
            attempts++;

            _cache.Set(key, attempts, TimeSpan.FromMinutes(15));

            await _logService.LogSecurityEventAsync(null, "FailedLogin",
                $"Failed login attempt for username: {username}", ipAddress);

            if (attempts >= 5)
            {
                await _logService.LogSecurityEventAsync(null, "SuspiciousActivity",
                    $"Multiple failed login attempts ({attempts}) from IP: {ipAddress}", ipAddress);

                _logger.LogWarning("Potential brute force attack detected from IP: {IpAddress}", ipAddress);
            }
        }

        public async Task MonitorSuspiciousActivityAsync(int userId, string activity, string ipAddress)
        {
            await _logService.LogSecurityEventAsync(userId, "SuspiciousActivity", activity, ipAddress);

            _logger.LogWarning("Suspicious activity detected for user {UserId} from IP {IpAddress}: {Activity}",
                userId, ipAddress, activity);
        }

        public async Task CheckRateLimitAsync(int userId, string endpoint)
        {
            var key = $"rate_limit_{userId}_{endpoint}";
            var requests = _cache.Get<int>(key);
            requests++;

            _cache.Set(key, requests, TimeSpan.FromMinutes(1));

            if (requests > 60) // حداکثر 60 درخواست در دقیقه
            {
                await _logService.LogSecurityEventAsync(userId, "RateLimitExceeded",
                    $"Rate limit exceeded for endpoint: {endpoint}");

                throw new InvalidOperationException("تعداد درخواست‌های شما از حد مجاز بیشتر است. لطفاً کمی صبر کنید.");
            }
        }
    }
}

