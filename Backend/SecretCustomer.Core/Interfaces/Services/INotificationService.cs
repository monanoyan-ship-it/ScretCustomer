using SecretCustomer.Core.DTOs.Notification;

namespace SecretCustomer.Core.Interfaces.Services;

public interface INotificationService
{
    Task<(List<NotificationListDto> Items, int TotalCount)> GetNotificationsAsync(int userId, NotificationFilterDto filter);
    Task<List<NotificationListDto>> GetUnreadNotificationsAsync(int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<NotificationDto?> GetNotificationAsync(int id, int userId);
    Task<int> CreateNotificationAsync(CreateNotificationDto dto, int senderUserId);
    Task<int> CreateBulkNotificationsAsync(CreateBulkNotificationDto dto, int senderUserId);
    Task<(bool Found, int UnreadCount)> MarkAsReadAsync(int id, int userId);
    Task<int> MarkAllAsReadAsync(int userId);
    Task<bool> DeleteNotificationAsync(int id, int userId);
    Task<NotificationSummaryDto> GetSummaryAsync(int userId);
    Task<List<NotificationSettingDto>> GetSettingsAsync(int userId);
    Task<(bool Success, string? ErrorKey)> UpdateSettingsAsync(int userId, UpdateNotificationSettingDto dto);
}
