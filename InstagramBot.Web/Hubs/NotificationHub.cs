using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace InstagramBot.Web.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            _logger.LogInformation("User {UserId} connected to NotificationHub", userId);

            // اضافه کردن کاربر به گروه مخصوص خودش
            await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

            // ارسال پیام خوش‌آمدگویی
            await Clients.Caller.SendAsync("ReceiveNotification",
                "به پنل مدیریت خوش آمدید",
                "Success");

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier;
            _logger.LogInformation("User {UserId} disconnected from NotificationHub", userId);

            // حذف کاربر از گروه
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");

            await base.OnDisconnectedAsync(exception);
        }

        // متد برای ارسال اعلان به کاربر خاص
        public async Task SendNotificationToUser(string userId, string message, string type)
        {
            await Clients.Group($"User_{userId}").SendAsync("ReceiveNotification", message, type);
        }

        // متد برای ارسال اعلان به همه کاربران
        public async Task SendNotificationToAll(string message, string type)
        {
            await Clients.All.SendAsync("ReceiveNotification", message, type);
        }

        // متد برای پیوستن به گروه خاص (مثلاً برای اعلان‌های مربوط به یک اکانت خاص)
        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserId} joined group {GroupName}",
                Context.UserIdentifier, groupName);
        }

        // متد برای خروج از گروه
        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
            _logger.LogInformation("User {UserId} left group {GroupName}",
                Context.UserIdentifier, groupName);
        }
    }
}
