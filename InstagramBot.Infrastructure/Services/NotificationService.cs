using InstagramBot.Application.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

// فرض بر این است که Hub شما در این namespace است
// using InstagramBot.Web.Hubs; 

namespace InstagramBot.Infrastructure.Services
{
    // یک HubContext ساختگی برای مثال، شما باید Hub واقعی خود را اینجا تزریق کنید
    public class NotificationHub : Hub { }

    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(IHubContext<NotificationHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task SendNotificationToUser(string userId, string message, NotificationType type)
        {
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", message, type.ToString());
        }

        public async Task SendNotificationToAll(string message, NotificationType type)
        {
            await _hubContext.Clients.All.SendAsync("ReceiveNotification", message, type.ToString());
        }
    }
}
