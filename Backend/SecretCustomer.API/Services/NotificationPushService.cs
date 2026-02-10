using Microsoft.AspNetCore.SignalR;
using SecretCustomer.API.Hubs;
using SecretCustomer.Core.DTOs.Notification;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Services;

/// <summary>
/// SignalR üzerinden real-time bildirim push implementasyonu
/// </summary>
public class NotificationPushService : INotificationPushService
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationPushService(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(int userId, NotificationListDto notification)
    {
        await _hubContext.Clients.Group($"user_{userId}")
            .SendAsync("ReceiveNotification", notification);
    }

    public async Task SendUnreadCountToUserAsync(int userId, int count)
    {
        await _hubContext.Clients.Group($"user_{userId}")
            .SendAsync("UpdateUnreadCount", count);
    }
}
