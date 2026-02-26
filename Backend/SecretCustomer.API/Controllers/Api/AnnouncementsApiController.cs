using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Announcement;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/announcements")]
[Authorize]
public class AnnouncementsApiController : BaseApiController
{
    private readonly IAnnouncementService _announcementService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILocalizationService _localizationService;
    private readonly INotificationCreatorService _notificationCreator;

    public AnnouncementsApiController(
        IAnnouncementService announcementService,
        IAuditLogService auditLogService,
        ILocalizationService localizationService,
        INotificationCreatorService notificationCreator,
        IConfiguration configuration) : base(configuration)
    {
        _announcementService = announcementService;
        _auditLogService = auditLogService;
        _localizationService = localizationService;
        _notificationCreator = notificationCreator;
    }

    /// <summary>
    /// Tüm aktif duyuruları listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeExpired = false)
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var announcements = await _announcementService.GetAllAsync(userRole, includeExpired);
            return Ok(announcements);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting announcements", "Announcements", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.LoadError"), ex));
        }
    }

    /// <summary>
    /// Dashboard için son duyuruları getir
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetForDashboard([FromQuery] int count = 5)
    {
        try
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var announcements = await _announcementService.GetForDashboardAsync(userRole, count);
            return Ok(announcements);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting dashboard announcements", "Announcements", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.LoadError"), ex));
        }
    }

    /// <summary>
    /// Tek duyuru detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var announcement = await _announcementService.GetByIdAsync(id);

            if (announcement == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.NotFound")));
            }

            return Ok(announcement);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting announcement {id}", "Announcements", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.LoadSingleError"), ex));
        }
    }

    /// <summary>
    /// Admin için tüm duyuruları getir (silinmemiş)
    /// </summary>
    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllAdmin()
    {
        try
        {
            var announcements = await _announcementService.GetAllAdminAsync();
            return Ok(announcements);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting all announcements for admin", "Announcements", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.LoadError"), ex));
        }
    }

    /// <summary>
    /// Duyuru oluştur
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateAnnouncementDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (int.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            var (announcement, notifyUserIds) = await _announcementService.CreateAsync(dto, userId);

            await _auditLogService.LogInfoAsync($"Announcement {announcement.Id} created by user {userId}", "Announcements");

            if (notifyUserIds.Any())
            {
                await _notificationCreator.CreateBulkAsync(
                    notifyUserIds,
                    NotificationTypes.Ids.Info,
                    announcement.Title,
                    announcement.Summary ?? (announcement.Content.Length > 100 ? announcement.Content.Substring(0, 100) + "..." : announcement.Content),
                    actionUrl: $"/Announcements",
                    relatedEntityId: announcement.Id,
                    relatedEntityType: "Announcement",
                    senderUserId: userId);
            }

            return Ok(announcement);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error creating announcement", "Announcements", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.CreateError"), ex));
        }
    }

    /// <summary>
    /// Duyuru güncelle
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateAnnouncementDto dto)
    {
        try
        {
            var (success, message) = await _announcementService.UpdateAsync(id, dto);

            if (!success && message == "NotFound")
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.NotFound")));
            }

            await _auditLogService.LogInfoAsync($"Announcement {id} updated", "Announcements");

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Announcement.UpdateSuccess") });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating announcement {id}", "Announcements", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.UpdateError"), ex));
        }
    }

    /// <summary>
    /// Duyuru sil (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var (success, message) = await _announcementService.DeleteAsync(id);

            if (!success && message == "NotFound")
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.NotFound")));
            }

            await _auditLogService.LogInfoAsync($"Announcement {id} deleted", "Announcements");

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Announcement.DeleteSuccess") });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error deleting announcement {id}", "Announcements", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.DeleteError"), ex));
        }
    }
}
