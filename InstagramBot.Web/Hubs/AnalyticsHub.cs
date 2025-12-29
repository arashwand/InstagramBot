using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using InstagramBot.Application.DTOs.Analytics;

namespace InstagramBot.Web.Hubs
{
    [Authorize]
    public class AnalyticsHub : Hub
    {
        private readonly ILogger<AnalyticsHub> _logger;

        public AnalyticsHub(ILogger<AnalyticsHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            _logger.LogInformation("User {UserId} connected to AnalyticsHub", userId);

            // اضافه کردن به گروه آنالیتیکس
            await Groups.AddToGroupAsync(Context.ConnectionId, "AnalyticsUsers");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            _logger.LogInformation("User {UserId} disconnected from AnalyticsHub", userId);

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "AnalyticsUsers");

            await base.OnDisconnectedAsync(exception);
        }

        // متد برای ارسال آمار بلادرنگ
        public async Task SendRealTimeAnalytics(object analyticsData)
        {
            await Clients.Group("AnalyticsUsers").SendAsync("ReceiveAnalyticsUpdate", analyticsData);
        }

        // متد برای ارسال آمار مربوط به اکانت خاص
        public async Task SendAccountAnalytics(int accountId, object analyticsData)
        {
            await Clients.Group($"Account_{accountId}").SendAsync("ReceiveAccountAnalytics", analyticsData);
        }

        // متد برای پیوستن به گروه مربوط به اکانت خاص
        public async Task JoinAccountGroup(int accountId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Account_{accountId}");
            _logger.LogInformation("User {UserId} joined account group {AccountId}",
                Context.UserIdentifier, accountId);
        }

        // متد برای خروج از گروه اکانت
        public async Task LeaveAccountGroup(int accountId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Account_{accountId}");
            _logger.LogInformation("User {UserId} left account group {AccountId}",
                Context.UserIdentifier, accountId);
        }

        // متد برای درخواست آمار فوری
        public async Task RequestInstantAnalytics(int accountId)
        {
            // اینجا می‌توانید سرویس آنالیتیکس را فراخوانی کنید
            // و نتیجه را به کاربر ارسال کنید
            await Clients.Caller.SendAsync("ReceiveInstantAnalytics",
                new { AccountId = accountId, Message = "درخواست آمار در حال پردازش..." });
        }
    }
}
