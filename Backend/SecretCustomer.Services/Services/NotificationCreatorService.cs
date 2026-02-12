using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Notification;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services;

/// <summary>
/// Merkezi bildirim oluşturma servisi
/// NotificationSettings'e göre InApp + Email kanallarını yönetir
/// </summary>
public class NotificationCreatorService : INotificationCreatorService
{
    private readonly ApplicationDbContext _context;
    private readonly INotificationPushService _pushService;
    private readonly IEmailService _emailService;
    private readonly IAuditLogService _auditLogService;

    public NotificationCreatorService(
        ApplicationDbContext context,
        INotificationPushService pushService,
        IEmailService emailService,
        IAuditLogService auditLogService)
    {
        _context = context;
        _pushService = pushService;
        _emailService = emailService;
        _auditLogService = auditLogService;
    }

    public async Task<int> CreateAsync(
        int recipientUserId,
        int notificationTypeId,
        string title,
        string message,
        string? actionUrl = null,
        int? relatedEntityId = null,
        string? relatedEntityType = null,
        int? priorityId = null,
        int? senderUserId = null)
    {
        var effectivePriorityId = priorityId ?? NotificationPriorities.Ids.Normal;

        // NotificationSettings kontrol et
        var setting = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == recipientUserId && s.NotificationTypeId == notificationTypeId);

        // Urgent ve System bildirimleri her zaman gönderilir
        var isForced = notificationTypeId == NotificationTypes.Ids.System
            || effectivePriorityId == NotificationPriorities.Ids.Urgent;

        var inAppEnabled = isForced || setting?.InAppEnabled != false;
        var emailEnabled = setting?.EmailEnabled == true;

        if (!inAppEnabled)
            return 0;

        // Notification entity oluştur
        var notification = new Notification
        {
            NotificationTypeId = notificationTypeId,
            ChannelId = NotificationChannels.Ids.InApp,
            PriorityId = effectivePriorityId,
            Title = title,
            Message = message,
            RecipientUserId = recipientUserId,
            SenderUserId = senderUserId,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            ActionUrl = actionUrl,
            IsSent = true,
            SentAt = TurkeyTime.Now
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // SignalR push
        try
        {
            var dto = new NotificationListDto
            {
                Id = notification.Id,
                NotificationType = NotificationTypes.GetById(notificationTypeId)?.SystemName ?? "Info",
                Priority = NotificationPriorities.GetById(effectivePriorityId)?.SystemName ?? "Normal",
                Title = title,
                Message = message,
                ActionUrl = actionUrl,
                IsRead = false,
                CreatedAt = notification.CreatedAt
            };

            await _pushService.SendToUserAsync(recipientUserId, dto);

            var unreadCount = await _context.Notifications
                .CountAsync(n => n.RecipientUserId == recipientUserId && !n.IsRead);
            await _pushService.SendUnreadCountToUserAsync(recipientUserId, unreadCount);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync(
                $"SignalR push hatası (userId: {recipientUserId}, notificationId: {notification.Id})",
                "NotificationCreator", ex);
        }

        // Email gönder (fire-and-forget)
        if (emailEnabled)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var user = await _context.Users.FindAsync(recipientUserId);
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        await _emailService.SendEmailAsync(user.Email, title, message);
                    }
                }
                catch
                {
                    // Email hatası sessizce geçilir
                }
            });
        }

        return notification.Id;
    }

    public async Task<int> CreateBulkAsync(
        IEnumerable<int> recipientUserIds,
        int notificationTypeId,
        string title,
        string message,
        string? actionUrl = null,
        int? relatedEntityId = null,
        string? relatedEntityType = null,
        int? priorityId = null,
        int? senderUserId = null)
    {
        var count = 0;
        foreach (var userId in recipientUserIds.Distinct())
        {
            var id = await CreateAsync(userId, notificationTypeId, title, message,
                actionUrl, relatedEntityId, relatedEntityType, priorityId, senderUserId);
            if (id > 0) count++;
        }
        return count;
    }
}
