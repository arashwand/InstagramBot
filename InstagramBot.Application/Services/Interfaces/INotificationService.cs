namespace InstagramBot.Application.Services.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationToUser(string userId, string message, NotificationType type);
        Task SendNotificationToAll(string message, NotificationType type);
        // ... متدهای دیگر برای مدیریت نوتیفیکیشن‌ها
    }

    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error
    }
}
