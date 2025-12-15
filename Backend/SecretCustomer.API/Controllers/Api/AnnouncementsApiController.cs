using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Announcement;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/announcements")]
[Authorize]
public class AnnouncementsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<AnnouncementsApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public AnnouncementsApiController(
        ApplicationDbContext context,
        ILogger<AnnouncementsApiController> logger,
        ILocalizationService localizationService)
    {
        _context = context;
        _logger = logger;
        _localizationService = localizationService;
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
            var now = DateTime.UtcNow;

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
                    Type = a.Type,
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
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Announcement.LoadError") });
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
            var now = DateTime.UtcNow;

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
                    Type = a.Type,
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
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Announcement.LoadError") });
        }
    }

    /// <summary>
    /// Tek duyuru detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var announcement = await _context.Announcements
                .Include(a => a.CreatedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (announcement == null)
            {
                return NotFound(new { message = await _localizationService.GetResourceAsync("Api.Announcement.NotFound") });
            }

            return Ok(new AnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content,
                Summary = announcement.Summary,
                Type = announcement.Type,
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
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Announcement.LoadSingleError") });
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
                    Type = a.Type,
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
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Announcement.LoadError") });
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
            Guid? userId = null;
            if (Guid.TryParse(userIdClaim, out var parsedUserId))
            {
                userId = parsedUserId;
            }

            var announcement = new Announcement
            {
                Title = dto.Title,
                Content = dto.Content,
                Summary = dto.Summary,
                Type = dto.Type,
                Priority = dto.Priority,
                PublishDate = dto.PublishDate ?? DateTime.UtcNow,
                ExpiryDate = dto.ExpiryDate,
                IsActive = dto.IsActive,
                IsPinned = dto.IsPinned,
                TargetRoles = dto.TargetRoles,
                CreatedByUserId = userId
            };

            _context.Announcements.Add(announcement);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement {Id} created by user {UserId}", announcement.Id, userId);

            return Ok(new AnnouncementDto
            {
                Id = announcement.Id,
                Title = announcement.Title,
                Content = announcement.Content,
                Summary = announcement.Summary,
                Type = announcement.Type,
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
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Announcement.CreateError") });
        }
    }

    /// <summary>
    /// Duyuru güncelle
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateAnnouncementDto dto)
    {
        try
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound(new { message = await _localizationService.GetResourceAsync("Api.Announcement.NotFound") });
            }

            announcement.Title = dto.Title;
            announcement.Content = dto.Content;
            announcement.Summary = dto.Summary;
            announcement.Type = dto.Type;
            announcement.Priority = dto.Priority;
            announcement.PublishDate = dto.PublishDate ?? announcement.PublishDate;
            announcement.ExpiryDate = dto.ExpiryDate;
            announcement.IsActive = dto.IsActive;
            announcement.IsPinned = dto.IsPinned;
            announcement.TargetRoles = dto.TargetRoles;
            announcement.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement {Id} updated", id);

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Announcement.UpdateSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating announcement {Id}", id);
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Announcement.UpdateError") });
        }
    }

    /// <summary>
    /// Duyuru sil (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var announcement = await _context.Announcements.FindAsync(id);
            if (announcement == null)
            {
                return NotFound(new { message = await _localizationService.GetResourceAsync("Api.Announcement.NotFound") });
            }

            announcement.IsDeleted = true;
            announcement.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Announcement {Id} deleted", id);

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Announcement.DeleteSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting announcement {Id}", id);
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Announcement.DeleteError") });
        }
    }
}
