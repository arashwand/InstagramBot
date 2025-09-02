using InstagramBot.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace InstagramBot.Application.Services
{
    public class TokenManagementBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TokenManagementBackgroundService> _logger;

        public TokenManagementBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<TokenManagementBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Token Management Background Service started");

            // زمان‌بندی اولیه
            using (var scope = _serviceProvider.CreateScope())
            {
                var tokenService = scope.ServiceProvider.GetRequiredService<ITokenManagementService>();
                await tokenService.ScheduleTokenRefreshAsync();
            }

            // اجرای دوره‌ای هر 6 ساعت برای بررسی وضعیت
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromHours(6), stoppingToken);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenManagementService>();

                        // بررسی توکن‌های منقضی‌شده
                        await tokenService.RefreshExpiredTokensAsync();
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in token management background service");
                }
            }

            _logger.LogInformation("Token Management Background Service stopped");
        }
    }
}

