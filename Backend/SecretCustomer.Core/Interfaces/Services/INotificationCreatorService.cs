namespace SecretCustomer.Core.Interfaces.Services;

/// <summary>
/// Merkezi bildirim oluşturma servisi - DB kayıt + SignalR push + Email
/// </summary>
public interface INotificationCreatorService
{
    /// <summary>
    /// Tek kullanıcıya bildirim oluştur
    /// </summary>
    Task<int> CreateAsync(
        int recipientUserId,
        int notificationTypeId,
        string title,
        string message,
        string? actionUrl = null,
        int? relatedEntityId = null,
        string? relatedEntityType = null,
        int? priorityId = null,
        int? senderUserId = null);

    /// <summary>
    /// Birden fazla kullanıcıya bildirim oluştur
    /// </summary>
    Task<int> CreateBulkAsync(
        IEnumerable<int> recipientUserIds,
        int notificationTypeId,
        string title,
        string message,
        string? actionUrl = null,
        int? relatedEntityId = null,
        string? relatedEntityType = null,
        int? priorityId = null,
        int? senderUserId = null);
}
