using SecretCustomer.Core.DTOs.Notification;

namespace SecretCustomer.Core.Interfaces.Services;

/// <summary>
/// SignalR üzerinden real-time bildirim push servisi
/// </summary>
public interface INotificationPushService
{
    Task SendToUserAsync(int userId, NotificationListDto notification);
    Task SendUnreadCountToUserAsync(int userId, int count);
}
