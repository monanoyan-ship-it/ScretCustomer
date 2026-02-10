using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Notification;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

/// <summary>
/// Bildirim yönetimi API controller
/// </summary>
[Route("api/notifications")]
[ApiController]
[Authorize]
public class NotificationsApiController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILocalizationService _localizationService;
    private readonly INotificationPushService _pushService;

    public NotificationsApiController(
        ApplicationDbContext context,
        ILocalizationService localizationService,
        INotificationPushService pushService,
        IConfiguration configuration) : base(configuration)
    {
        _context = context;
        _localizationService = localizationService;
        _pushService = pushService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Bildirimleri listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] NotificationFilterDto filter)
    {
        var userId = GetCurrentUserId();

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
            var datePredicates = filter.DateRanges.Select(dr =>
            {
                DateTime? startUtc = dr.StartDate.HasValue
                    ? DateTime.SpecifyKind(dr.StartDate.Value.Date, DateTimeKind.Utc)
                    : null;
                DateTime? endUtc = dr.EndDate.HasValue
                    ? DateTime.SpecifyKind(dr.EndDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc)
                    : null;
                return (Start: startUtc, End: endUtc);
            }).ToList();

            var minStart = datePredicates.Where(d => d.Start.HasValue).Select(d => d.Start!.Value).DefaultIfEmpty(DateTime.MinValue).Min();
            var maxEnd = datePredicates.Where(d => d.End.HasValue).Select(d => d.End!.Value).DefaultIfEmpty(DateTime.MaxValue).Max();

            if (minStart != DateTime.MinValue)
                query = query.Where(n => n.CreatedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(n => n.CreatedAt <= maxEnd);
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

        return Ok(new { items = notifications, totalCount });
    }

    /// <summary>
    /// Okunmamış bildirimler
    /// </summary>
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        var userId = GetCurrentUserId();

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

        return Ok(notifications);
    }

    /// <summary>
    /// Okunmamış bildirim sayısı
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _context.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
        return Ok(new { count });
    }

    /// <summary>
    /// Bildirim detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNotification(int id)
    {
        var userId = GetCurrentUserId();

        var notification = await _context.Notifications
            .Include(n => n.SenderUser)
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);

        if (notification == null)
            return NotFound();

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

        return Ok(dto);
    }

    /// <summary>
    /// Bildirim oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
    {
        var notification = new Notification
        {
            NotificationTypeId = NotificationTypes.GetBySystemName(dto.NotificationType)?.Id ?? NotificationTypes.Ids.Info,
            ChannelId = NotificationChannels.GetBySystemName(dto.Channel)?.Id ?? NotificationChannels.Ids.InApp,
            PriorityId = NotificationPriorities.GetBySystemName(dto.Priority)?.Id ?? NotificationPriorities.Ids.Normal,
            Title = dto.Title,
            Message = dto.Message,
            RecipientUserId = dto.RecipientUserId,
            SenderUserId = GetCurrentUserId(),
            RelatedEntityId = dto.RelatedEntityId,
            RelatedEntityType = dto.RelatedEntityType,
            ActionUrl = dto.ActionUrl,
            ScheduledAt = dto.ScheduledAt,
            ExpiresAt = dto.ExpiresAt,
            GroupId = dto.GroupId,
            IsSent = dto.ScheduledAt == null || dto.ScheduledAt <= DateTime.UtcNow,
            SentAt = dto.ScheduledAt == null || dto.ScheduledAt <= DateTime.UtcNow ? DateTime.UtcNow : null
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return Ok(new { id = notification.Id });
    }

    /// <summary>
    /// Toplu bildirim oluştur
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> CreateBulkNotifications([FromBody] CreateBulkNotificationDto dto)
    {
        var notificationTypeId = NotificationTypes.GetBySystemName(dto.NotificationType)?.Id ?? NotificationTypes.Ids.Info;
        var channelId = NotificationChannels.GetBySystemName(dto.Channel)?.Id ?? NotificationChannels.Ids.InApp;
        var priorityId = NotificationPriorities.GetBySystemName(dto.Priority)?.Id ?? NotificationPriorities.Ids.Normal;
        var senderId = GetCurrentUserId();
        var groupId = Guid.NewGuid().ToString();
        var isSent = dto.ScheduledAt == null || dto.ScheduledAt <= DateTime.UtcNow;
        var sentAt = isSent ? DateTime.UtcNow : (DateTime?)null;

        var notifications = dto.RecipientUserIds.Select(recipientId => new Notification
        {
            NotificationTypeId = notificationTypeId,
            ChannelId = channelId,
            PriorityId = priorityId,
            Title = dto.Title,
            Message = dto.Message,
            RecipientUserId = recipientId,
            SenderUserId = senderId,
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

        return Ok(new { count = notifications.Count });
    }

    /// <summary>
    /// Bildirimi okundu olarak işaretle
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);

        if (notification == null)
            return NotFound();

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            // SignalR ile unread count güncelle
            var unreadCount = await _context.Notifications.CountAsync(n => n.RecipientUserId == userId && !n.IsRead);
            await _pushService.SendUnreadCountToUserAsync(userId, unreadCount);
        }

        return Ok();
    }

    /// <summary>
    /// Tüm bildirimleri okundu olarak işaretle
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();

        var notifications = await _context.Notifications
            .Where(n => n.RecipientUserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var notification in notifications)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // SignalR ile unread count güncelle
        await _pushService.SendUnreadCountToUserAsync(userId, 0);

        return Ok(new { count = notifications.Count });
    }

    /// <summary>
    /// Bildirimi sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var userId = GetCurrentUserId();

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == userId);

        if (notification == null)
            return NotFound();

        notification.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Bildirim özeti
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetCurrentUserId();
        var today = DateTime.UtcNow.Date;

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

        return Ok(summary);
    }

    /// <summary>
    /// Bildirim ayarlarını getir
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var userId = GetCurrentUserId();

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

        return Ok(settings);
    }

    /// <summary>
    /// Bildirim ayarlarını güncelle
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateNotificationSettingDto dto)
    {
        var userId = GetCurrentUserId();

        var notificationTypeId = NotificationTypes.GetBySystemName(dto.NotificationType)?.Id;
        if (!notificationTypeId.HasValue)
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Notification.InvalidType")));

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

        return Ok();
    }
}
