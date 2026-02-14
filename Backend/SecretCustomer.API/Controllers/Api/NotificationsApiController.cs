using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Notification;
using SecretCustomer.Core.Interfaces.Services;
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
    private readonly INotificationService _notificationService;
    private readonly ILocalizationService _localizationService;
    private readonly INotificationPushService _pushService;

    public NotificationsApiController(
        INotificationService notificationService,
        ILocalizationService localizationService,
        INotificationPushService pushService,
        IConfiguration configuration) : base(configuration)
    {
        _notificationService = notificationService;
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
        var (items, totalCount) = await _notificationService.GetNotificationsAsync(userId, filter);
        return Ok(new { items, totalCount });
    }

    /// <summary>
    /// Okunmamış bildirimler
    /// </summary>
    [HttpGet("unread")]
    public async Task<IActionResult> GetUnreadNotifications()
    {
        var userId = GetCurrentUserId();
        var notifications = await _notificationService.GetUnreadNotificationsAsync(userId);
        return Ok(notifications);
    }

    /// <summary>
    /// Okunmamış bildirim sayısı
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetCurrentUserId();
        var count = await _notificationService.GetUnreadCountAsync(userId);
        return Ok(new { count });
    }

    /// <summary>
    /// Bildirim detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetNotification(int id)
    {
        var userId = GetCurrentUserId();
        var dto = await _notificationService.GetNotificationAsync(id, userId);

        if (dto == null)
            return NotFound();

        return Ok(dto);
    }

    /// <summary>
    /// Bildirim oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateNotification([FromBody] CreateNotificationDto dto)
    {
        var notificationId = await _notificationService.CreateNotificationAsync(dto, GetCurrentUserId());
        return Ok(new { id = notificationId });
    }

    /// <summary>
    /// Toplu bildirim oluştur
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> CreateBulkNotifications([FromBody] CreateBulkNotificationDto dto)
    {
        var count = await _notificationService.CreateBulkNotificationsAsync(dto, GetCurrentUserId());
        return Ok(new { count });
    }

    /// <summary>
    /// Bildirimi okundu olarak işaretle
    /// </summary>
    [HttpPost("{id}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetCurrentUserId();
        var (found, unreadCount) = await _notificationService.MarkAsReadAsync(id, userId);

        if (!found)
            return NotFound();

        // SignalR ile unread count güncelle
        await _pushService.SendUnreadCountToUserAsync(userId, unreadCount);

        return Ok();
    }

    /// <summary>
    /// Tüm bildirimleri okundu olarak işaretle
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetCurrentUserId();
        var count = await _notificationService.MarkAllAsReadAsync(userId);

        // SignalR ile unread count güncelle
        await _pushService.SendUnreadCountToUserAsync(userId, 0);

        return Ok(new { count });
    }

    /// <summary>
    /// Bildirimi sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        var userId = GetCurrentUserId();
        var deleted = await _notificationService.DeleteNotificationAsync(id, userId);

        if (!deleted)
            return NotFound();

        return Ok();
    }

    /// <summary>
    /// Bildirim özeti
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetCurrentUserId();
        var summary = await _notificationService.GetSummaryAsync(userId);
        return Ok(summary);
    }

    /// <summary>
    /// Bildirim ayarlarını getir
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var userId = GetCurrentUserId();
        var settings = await _notificationService.GetSettingsAsync(userId);
        return Ok(settings);
    }

    /// <summary>
    /// Bildirim ayarlarını güncelle
    /// </summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateNotificationSettingDto dto)
    {
        var userId = GetCurrentUserId();
        var (success, errorKey) = await _notificationService.UpdateSettingsAsync(userId, dto);

        if (!success)
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync(errorKey!)));

        return Ok();
    }
}
