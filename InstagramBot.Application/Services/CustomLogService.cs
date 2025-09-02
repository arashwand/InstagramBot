using InstagramBot.Application.Services.Interfaces;
using InstagramBot.Core.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramBot.Application.Services
{
    public class CustomLogService : ICustomLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CustomLogService> _logger;

        public CustomLogService(ApplicationDbContext context, ILogger<CustomLogService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task LogUserActivityAsync(int userId, string activity, string details = null, string ipAddress = null)
        {
            var log = new Log
            {
                Timestamp = DateTime.UtcNow,
                Level = "Information",
                Message = $"User Activity: {activity}",
                Source = "UserActivity",
                UserId = userId,
                IpAddress = ipAddress
            };

            if (!string.IsNullOrEmpty(details))
            {
                log.Message += $" - Details: {details}";
            }

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} performed activity: {Activity} from IP: {IpAddress}",
                userId, activity, ipAddress);
        }

        public async Task LogSecurityEventAsync(int? userId, string eventType, string description, string ipAddress = null)
        {
            var log = new Log
            {
                Timestamp = DateTime.UtcNow,
                Level = "Warning",
                Message = $"Security Event: {eventType} - {description}",
                Source = "Security",
                UserId = userId,
                IpAddress = ipAddress
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();

            _logger.LogWarning("Security event {EventType} for user {UserId} from IP {IpAddress}: {Description}",
                eventType, userId, ipAddress, description);
        }

        public async Task LogApiCallAsync(string endpoint, string method, int? userId, bool success, string errorMessage = null)
        {
            var log = new Log
            {
                Timestamp = DateTime.UtcNow,
                Level = success ? "Information" : "Error",
                Message = $"API Call: {method} {endpoint} - {(success ? "Success" : "Failed")}",
                Source = "ApiCall",
                UserId = userId,
                Exception = errorMessage
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();

            if (success)
            {
                _logger.LogInformation("API call {Method} {Endpoint} by user {UserId} succeeded",
                    method, endpoint, userId);
            }
            else
            {
                _logger.LogError("API call {Method} {Endpoint} by user {UserId} failed: {Error}",
                    method, endpoint, userId, errorMessage);
            }
        }

        public async Task LogInstagramApiCallAsync(int accountId, string endpoint, bool success, string errorMessage = null)
        {
            var log = new Log
            {
                Timestamp = DateTime.UtcNow,
                Level = success ? "Information" : "Error",
                Message = $"Instagram API Call: {endpoint} for account {accountId} - {(success ? "Success" : "Failed")}",
                Source = "InstagramApi",
                Exception = errorMessage
            };

            _context.Logs.Add(log);
            await _context.SaveChangesAsync();

            if (success)
            {
                _logger.LogInformation("Instagram API call {Endpoint} for account {AccountId} succeeded",
                    endpoint, accountId);
            }
            else
            {
                _logger.LogError("Instagram API call {Endpoint} for account {AccountId} failed: {Error}",
                    endpoint, accountId, errorMessage);
            }
        }
    }
}

