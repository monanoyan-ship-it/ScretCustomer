using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Announcement;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Security.Claims;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/announcements")]
[Authorize]
public class AnnouncementsApiController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnnouncementsApiController> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly INotificationCreatorService _notificationCreator;

    public AnnouncementsApiController(
        ApplicationDbContext context,
        ILogger<AnnouncementsApiController> logger,
        ILocalizationService localizationService,
        INotificationCreatorService notificationCreator,
        IConfiguration configuration) : base(configuration)
    {
        _context = context;
        _logger = logger;
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
            var now = TurkeyTime.Now;

            var query = _context.Announcements
                .Include(a => a.CreatedByUser)
                .Where(a => a.IsActive)
                .AsQueryable();

            if (!includeExpired)
            {
                query = query.Where(a => a.ExpiryDate == null || a.ExpiryDate > now);
            }

            var announcements = await query
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.Priority)
                .ThenByDescending(a => a.PublishDate)
                .Select(a => new AnnouncementDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Content = a.Content,
                    Summary = a.Summary,
                    TypeId = a.TypeId,
                    Priority = a.Priority,
                    PublishDate = a.PublishDate,
                    ExpiryDate = a.ExpiryDate,
                    IsActive = a.IsActive,
                    IsPinned = a.IsPinned,
                    TargetRoles = a.TargetRoles,
                    CreatedByUserName = a.CreatedByUser != null
                        ? a.CreatedByUser.FirstName + " " + a.CreatedByUser.LastName
                        : null,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            // Rol bazlı filtreleme
            announcements = announcements
                .Where(a => string.IsNullOrEmpty(a.TargetRoles) ||
                           a.TargetRoles.Split(',').Any(r => r.Trim().Equals(userRole, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            return Ok(announcements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting announcements");
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
            var now = TurkeyTime.Now;

            var announcements = await _context.Announcements
                .Where(a => a.IsActive)
                .Where(a => a.PublishDate <= now)
                .Where(a => a.ExpiryDate == null || a.ExpiryDate > now)
                .OrderByDescending(a => a.IsPinned)
                .ThenByDescending(a => a.Priority)
                .ThenByDescending(a => a.PublishDate)
                .Take(count * 2) // Rol filtresi sonrası yeterli olsun diye fazla al
                .Select(a => new AnnouncementSummaryDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Summary = a.Summary ?? (a.Content.Length > 100 ? a.Content.Substring(0, 100) + "..." : a.Content),
                    TypeId = a.TypeId,
                    IsPinned = a.IsPinned,
                    PublishDate = a.PublishDate
                })
                .ToListAsync();

            // Rol filtresi
            var filteredAnnouncements = announcements.Take(count).ToList();

            return Ok(filteredAnnouncements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard announcements");
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
            var announcement = await _context.Announcements
                .Include(a => a.CreatedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.NotFound")));
            }

            return Ok(new AnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content,
                Summary = announcement.Summary,
                TypeId = announcement.TypeId,
                Priority = announcement.Priority,
                PublishDate = announcement.PublishDate,
                ExpiryDate = announcement.ExpiryDate,
                IsActive = announcement.IsActive,
                IsPinned = announcement.IsPinned,
                TargetRoles = announcement.TargetRoles,
                CreatedByUserName = announcement.CreatedByUser != null
                    ? announcement.CreatedByUser.FirstName + " " + announcement.CreatedByUser.LastName
                    : null,
                CreatedAt = announcement.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting announcement {Id}", id);
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
            var announcements = await _context.Announcements
                .Include(a => a.CreatedByUser)
                .OrderByDescending(a => a.CreatedAt)
                .Select(a => new AnnouncementDto
                {
                    Id = a.Id,
                    Title = a.Title,
                    Content = a.Content,
                    Summary = a.Summary,
                    TypeId = a.TypeId,
                    Priority = a.Priority,
                    PublishDate = a.PublishDate,
                    ExpiryDate = a.ExpiryDate,
                    IsActive = a.IsActive,
                    IsPinned = a.IsPinned,
                    TargetRoles = a.TargetRoles,
                    CreatedByUserName = a.CreatedByUser != null
                        ? a.CreatedByUser.FirstName + " " + a.CreatedByUser.LastName
                        : null,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(announcements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all announcements for admin");
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

            var announcement = new Announcement
            {
                Title = dto.Title,
                Content = dto.Content,
                Summary = dto.Summary,
                TypeId = dto.TypeId,
                Priority = dto.Priority,
                PublishDate = dto.PublishDate ?? TurkeyTime.Now,
                ExpiryDate = dto.ExpiryDate,
                IsActive = dto.IsActive,
                IsPinned = dto.IsPinned,
                TargetRoles = dto.TargetRoles,
                CreatedByUserId = userId
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement {Id} created by user {UserId}", announcement.Id, userId);

            // TargetRoles'a uyan kullanıcılara bildirim gönder
            var targetRoles = announcement.TargetRoles?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>();

            // System Users
            var userQuery = _context.Users.Where(u => u.IsActive && !u.IsDeleted);
            if (targetRoles.Length > 0)
            {
                var targetRoleIds = targetRoles
                    .Select(r => UserRoles.GetBySystemName(r)?.Id)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();
                if (targetRoleIds.Any())
                    userQuery = userQuery.Where(u => targetRoleIds.Contains(u.RoleId));
                else
                    userQuery = userQuery.Where(u => false); // Hiçbir system role eşleşmediyse skip
            }
            var userIds = await userQuery.Select(u => u.Id).ToListAsync();

            // Customer Personnel
            var cpQuery = _context.CustomerPersonnel.Where(cp => cp.IsActive && !cp.IsDeleted);
            if (targetRoles.Length > 0)
            {
                var cpRoleIds = targetRoles
                    .Select(r => CustomerPersonnelRoles.GetBySystemName(r)?.Id)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToList();
                if (cpRoleIds.Any())
                    cpQuery = cpQuery.Where(cp => cpRoleIds.Contains(cp.RoleId));
                else
                    cpQuery = cpQuery.Where(cp => false);
            }
            // CustomerPersonnel bildirimi: Notification tablosu RecipientUserId (int) → User tablosu
            // CustomerPersonnel'e bildirim göndermek için ayrı bir mekanizma gerekir
            // Şimdilik sadece system user'lara gönder

            // Oluşturanı hariç tut
            if (userId.HasValue)
                userIds = userIds.Where(id => id != userId.Value).ToList();

            if (userIds.Any())
            {
                await _notificationCreator.CreateBulkAsync(
                    userIds,
                    NotificationTypes.Ids.Info,
                    announcement.Title,
                    announcement.Summary ?? (announcement.Content.Length > 100 ? announcement.Content.Substring(0, 100) + "..." : announcement.Content),
                    actionUrl: $"/Announcements",
                    relatedEntityId: announcement.Id,
                    relatedEntityType: "Announcement",
                    senderUserId: userId);
            }

            return Ok(new AnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content,
                Summary = announcement.Summary,
                TypeId = announcement.TypeId,
                Priority = announcement.Priority,
                PublishDate = announcement.PublishDate,
                ExpiryDate = announcement.ExpiryDate,
                IsActive = announcement.IsActive,
                IsPinned = announcement.IsPinned,
                TargetRoles = announcement.TargetRoles,
                CreatedAt = announcement.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating announcement");
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
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.NotFound")));
            }

            announcement.Title = dto.Title;
            announcement.Content = dto.Content;
            announcement.Summary = dto.Summary;
            announcement.TypeId = dto.TypeId;
            announcement.Priority = dto.Priority;
            announcement.PublishDate = dto.PublishDate ?? announcement.PublishDate;
            announcement.ExpiryDate = dto.ExpiryDate;
            announcement.IsActive = dto.IsActive;
            announcement.IsPinned = dto.IsPinned;
            announcement.TargetRoles = dto.TargetRoles;
            announcement.UpdatedAt = TurkeyTime.Now;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement {Id} updated", id);

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Announcement.UpdateSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating announcement {Id}", id);
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
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.NotFound")));
            }

            announcement.IsDeleted = true;
            announcement.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement {Id} deleted", id);

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Announcement.DeleteSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting announcement {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Announcement.DeleteError"), ex));
        }
    }
}
