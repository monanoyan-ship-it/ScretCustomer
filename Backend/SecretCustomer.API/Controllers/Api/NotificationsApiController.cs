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

    public NotificationsApiController(ApplicationDbContext context, ILocalizationService localizationService, IConfiguration configuration) : base(configuration)
    {
        _context = context;
        _localizationService = localizationService;
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

        if (!string.IsNullOrEmpty(filter.NotificationType) && Enum.TryParse<NotificationType>(filter.NotificationType, out var notificationType))
            query = query.Where(n => n.NotificationType == notificationType);

        if (!string.IsNullOrEmpty(filter.Priority) && Enum.TryParse<NotificationPriority>(filter.Priority, out var priority))
            query = query.Where(n => n.Priority == priority);

        if (filter.IsRead.HasValue)
            query = query.Where(n => n.IsRead == filter.IsRead.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(n => n.CreatedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(n => n.CreatedAt <= filter.EndDate.Value);

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
                NotificationType = n.NotificationType.ToString(),
                Priority = n.Priority.ToString(),
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
            .OrderByDescending(n => n.Priority)
            .ThenByDescending(n => n.CreatedAt)
            .Take(10)
            .Select(n => new NotificationListDto
            {
                Id = n.Id,
                NotificationType = n.NotificationType.ToString(),
                Priority = n.Priority.ToString(),
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
            NotificationType = notification.NotificationType.ToString(),
            Channel = notification.Channel.ToString(),
            Priority = notification.Priority.ToString(),
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
            NotificationType = Enum.TryParse<NotificationType>(dto.NotificationType, out var notificationType) ? notificationType : NotificationType.Info,
            Channel = Enum.TryParse<NotificationChannel>(dto.Channel, out var channel) ? channel : NotificationChannel.InApp,
            Priority = Enum.TryParse<NotificationPriority>(dto.Priority, out var priority) ? priority : NotificationPriority.Normal,
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
        var notifications = dto.RecipientUserIds.Select(recipientId => new Notification
        {
            NotificationType = Enum.TryParse<NotificationType>(dto.NotificationType, out var notificationType) ? notificationType : NotificationType.Info,
            Channel = Enum.TryParse<NotificationChannel>(dto.Channel, out var channel) ? channel : NotificationChannel.InApp,
            Priority = Enum.TryParse<NotificationPriority>(dto.Priority, out var priority) ? priority : NotificationPriority.Normal,
            Title = dto.Title,
            Message = dto.Message,
            RecipientUserId = recipientId,
            SenderUserId = GetCurrentUserId(),
            RelatedEntityId = dto.RelatedEntityId,
            RelatedEntityType = dto.RelatedEntityType,
            ActionUrl = dto.ActionUrl,
            ScheduledAt = dto.ScheduledAt,
            GroupId = Guid.NewGuid().ToString(),
            IsSent = dto.ScheduledAt == null || dto.ScheduledAt <= DateTime.UtcNow,
            SentAt = dto.ScheduledAt == null || dto.ScheduledAt <= DateTime.UtcNow ? DateTime.UtcNow : null
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
                    NotificationType = n.NotificationType.ToString(),
                    Priority = n.Priority.ToString(),
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
                NotificationType = s.NotificationType.ToString(),
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

        if (!Enum.TryParse<NotificationType>(dto.NotificationType, out var notificationType))
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Notification.InvalidType")));

        var setting = await _context.NotificationSettings
            .FirstOrDefaultAsync(s => s.UserId == userId && s.NotificationType == notificationType);

        if (setting == null)
        {
            setting = new NotificationSetting
            {
                UserId = userId,
                NotificationType = notificationType
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
