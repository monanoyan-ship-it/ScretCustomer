using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Notification;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class NotificationService : INotificationService
{
    private readonly ApplicationDbContext _context;

    public NotificationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(List<NotificationListDto> Items, int TotalCount)> GetNotificationsAsync(int userId, NotificationFilterDto filter)
    {
        var query = _context.Notifications
            .Where(n => n.RecipientUserId == userId)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.NotificationType))
        {
            var notificationTypeId = NotificationTypes.GetBySystemName(filter.NotificationType)?.Id;
            if (notificationTypeId.HasValue)
                query = query.Where(n => n.NotificationTypeId == notificationTypeId.Value);
        }

        if (!string.IsNullOrEmpty(filter.Priority))
        {
            var priorityId = NotificationPriorities.GetBySystemName(filter.Priority)?.Id;
            if (priorityId.HasValue)
                query = query.Where(n => n.PriorityId == priorityId.Value);
        }

        if (filter.IsRead.HasValue)
            query = query.Where(n => n.IsRead == filter.IsRead.Value);

        // DateRanges pattern
        if (filter.DateRanges?.Any() == true)
        {
            query = DateRangeHelper.ApplyOrFilter(query, filter.DateRanges, "CreatedAt");
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(n => n.Title.ToLower().Contains(searchTerm) ||
                                     n.Message.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        var notifications = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(n => new NotificationListDto
            {
                Id = n.Id,
                NotificationType = NotificationTypes.GetById(n.NotificationTypeId)!.SystemName,
                Priority = NotificationPriorities.GetById(n.PriorityId)!.SystemName,
                Title = n.Title,
                Message = n.Message,
                ActionUrl = n.ActionUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return (notifications, totalCount);
    }

    public async Task<List<NotificationListDto>> GetUnreadNotificationsAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .OrderByDescending(n => n.PriorityId)
            .ThenByDescending(n => n.CreatedAt)
            .Take(10)
            .Select(n => new NotificationListDto
            {
                Id = n.Id,
                NotificationType = NotificationTypes.GetById(n.NotificationTypeId)!.SystemName,
                Priority = NotificationPriorities.GetById(n.PriorityId)!.SystemName,
                Title = n.Title,
                Message = n.Message,
                ActionUrl = n.ActionUrl,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync();

        return notifications;
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
    }

    public async Task<NotificationDto?> GetNotificationAsync(int id, int userId)
    {
        var notification = await _context.Notifications
            .Include(n => n.SenderUser)
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);

        if (notification == null)
            return null;

        var dto = new NotificationDto
        {
            Id = notification.Id,
            NotificationType = NotificationTypes.GetById(notification.NotificationTypeId)?.SystemName ?? "",
            Channel = NotificationChannels.GetById(notification.ChannelId)?.SystemName ?? "",
            Priority = NotificationPriorities.GetById(notification.PriorityId)?.SystemName ?? "",
            Title = notification.Title,
            Message = notification.Message,
            RecipientUserId = notification.RecipientUserId,
            SenderUserId = notification.SenderUserId,
            SenderUserName = notification.SenderUser != null ? $"{notification.SenderUser.FirstName} {notification.SenderUser.LastName}" : null,
            RelatedEntityId = notification.RelatedEntityId,
            RelatedEntityType = notification.RelatedEntityType,
            ActionUrl = notification.ActionUrl,
            IsRead = notification.IsRead,
            ReadAt = notification.ReadAt,
            IsSent = notification.IsSent,
            SentAt = notification.SentAt,
            ScheduledAt = notification.ScheduledAt,
            ExpiresAt = notification.ExpiresAt,
            CreatedAt = notification.CreatedAt
        };

        return dto;
    }

    public async Task<int> CreateNotificationAsync(CreateNotificationDto dto, int senderUserId)
    {
        var notification = new Notification
        {
            NotificationTypeId = NotificationTypes.GetBySystemName(dto.NotificationType)?.Id ?? NotificationTypes.Ids.Info,
            ChannelId = NotificationChannels.GetBySystemName(dto.Channel)?.Id ?? NotificationChannels.Ids.InApp,
            PriorityId = NotificationPriorities.GetBySystemName(dto.Priority)?.Id ?? NotificationPriorities.Ids.Normal,
            Title = dto.Title,
            Message = dto.Message,
            RecipientUserId = dto.RecipientUserId,
            SenderUserId = senderUserId,
            RelatedEntityId = dto.RelatedEntityId,
            RelatedEntityType = dto.RelatedEntityType,
            ActionUrl = dto.ActionUrl,
            ScheduledAt = dto.ScheduledAt,
            ExpiresAt = dto.ExpiresAt,
            GroupId = dto.GroupId,
            IsSent = dto.ScheduledAt == null || dto.ScheduledAt <= TurkeyTime.Now,
            SentAt = dto.ScheduledAt == null || dto.ScheduledAt <= TurkeyTime.Now ? TurkeyTime.Now : null
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return notification.Id;
    }

    public async Task<int> CreateBulkNotificationsAsync(CreateBulkNotificationDto dto, int senderUserId)
    {
        var notificationTypeId = NotificationTypes.GetBySystemName(dto.NotificationType)?.Id ?? NotificationTypes.Ids.Info;
        var channelId = NotificationChannels.GetBySystemName(dto.Channel)?.Id ?? NotificationChannels.Ids.InApp;
        var priorityId = NotificationPriorities.GetBySystemName(dto.Priority)?.Id ?? NotificationPriorities.Ids.Normal;
        var groupId = Guid.NewGuid().ToString();
        var isSent = dto.ScheduledAt == null || dto.ScheduledAt <= TurkeyTime.Now;
        var sentAt = isSent ? TurkeyTime.Now : (DateTime?)null;

        var notifications = dto.RecipientUserIds.Select(recipientId => new Notification
        {
            NotificationTypeId = notificationTypeId,
            ChannelId = channelId,
            PriorityId = priorityId,
            Title = dto.Title,
            Message = dto.Message,
            RecipientUserId = recipientId,
            SenderUserId = senderUserId,
            RelatedEntityId = dto.RelatedEntityId,
            RelatedEntityType = dto.RelatedEntityType,
            ActionUrl = dto.ActionUrl,
            ScheduledAt = dto.ScheduledAt,
            GroupId = groupId,
            IsSent = isSent,
            SentAt = sentAt
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        return notifications.Count;
    }

    public async Task<(bool Found, int UnreadCount)> MarkAsReadAsync(int id, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);

        if (notification == null)
            return (false, 0);

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }

        // SignalR ile unread count güncelle
        var unreadCount = await _context.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
        return (true, unreadCount);
    }

    public async Task<int> MarkAllAsReadAsync(int userId)
    {
        var notifications = await _context.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = TurkeyTime.Now;
        }

        await _context.SaveChangesAsync();

        return notifications.Count;
    }

    public async Task<bool> DeleteNotificationAsync(int id, int userId)
    {
        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);

        if (notification == null)
            return false;

        notification.IsDeleted = true;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<NotificationSummaryDto> GetSummaryAsync(int userId)
    {
        var today = TurkeyTime.Now.Date;

        var summary = new NotificationSummaryDto
        {
            TotalNotifications = await _context.Notifications.CountAsync(n => n.RecipientUserId == userId),
            UnreadCount = await _context.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead),
            TodayCount = await _context.Notifications.CountAsync(n => n.RecipientUserId == userId && n.CreatedAt.Date == today),
            RecentNotifications = await _context.Notifications
                .Where(n => n.RecipientUserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new NotificationListDto
                {
                    Id = n.Id,
                    NotificationType = NotificationTypes.GetById(n.NotificationTypeId)!.SystemName,
                    Priority = NotificationPriorities.GetById(n.PriorityId)!.SystemName,
                    Title = n.Title,
                    Message = n.Message,
                    ActionUrl = n.ActionUrl,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync()
        };

        return summary;
    }

    public async Task<List<NotificationSettingDto>> GetSettingsAsync(int userId)
    {
        var settings = await _context.NotificationSettings
            .Where(s => s.UserId == userId)
            .Select(s => new NotificationSettingDto
            {
                Id = s.Id,
                UserId = s.UserId,
                NotificationType = NotificationTypes.GetById(s.NotificationTypeId)!.SystemName,
                InAppEnabled = s.InAppEnabled,
                EmailEnabled = s.EmailEnabled,
                SmsEnabled = s.SmsEnabled,
                PushEnabled = s.PushEnabled
            })
            .ToListAsync();

        return settings;
    }

    public async Task<(bool Success, string? ErrorKey)> UpdateSettingsAsync(int userId, UpdateNotificationSettingDto dto)
    {
        var notificationTypeId = NotificationTypes.GetBySystemName(dto.NotificationType)?.Id;
        if (!notificationTypeId.HasValue)
            return (false, "Api.Notification.InvalidType");

        var setting = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.NotificationTypeId == notificationTypeId.Value);

        if (setting == null)
        {
            setting = new NotificationSetting
            {
                UserId = userId,
                NotificationTypeId = notificationTypeId.Value
            };
            _context.NotificationSettings.Add(setting);
        }

        setting.InAppEnabled = dto.InAppEnabled;
        setting.EmailEnabled = dto.EmailEnabled;
        setting.SmsEnabled = dto.SmsEnabled;
        setting.PushEnabled = dto.PushEnabled;

        await _context.SaveChangesAsync();

        return (true, null);
    }
}
