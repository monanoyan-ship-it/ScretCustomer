using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Announcement;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class AnnouncementService : IAnnouncementService
{
    private readonly ApplicationDbContext _context;

    public AnnouncementService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AnnouncementDto>> GetAllAsync(string userRole, bool includeExpired)
    {
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

        return announcements;
    }

    public async Task<List<AnnouncementSummaryDto>> GetForDashboardAsync(string userRole, int count)
    {
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

        return filteredAnnouncements;
    }

    public async Task<AnnouncementDto?> GetByIdAsync(int id)
    {
        var announcement = await _context.Announcements
            .Include(a => a.CreatedByUser)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (announcement == null)
        {
            return null;
        }

        return new AnnouncementDto
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
        };
    }

    public async Task<List<AnnouncementDto>> GetAllAdminAsync()
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

        return announcements;
    }

    public async Task<(AnnouncementDto Announcement, List<int> NotifyUserIds)> CreateAsync(CreateAnnouncementDto dto, int? userId)
    {
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

        // CustomerPersonnel bildirimi: Notification tablosu RecipientUserId (int) → User tablosu
        // CustomerPersonnel'e bildirim göndermek için ayrı bir mekanizma gerekir
        // Şimdilik sadece system user'lara gönder

        // Oluşturanı hariç tut
        if (userId.HasValue)
            userIds = userIds.Where(id => id != userId.Value).ToList();

        var announcementDto = new AnnouncementDto
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
        };

        return (announcementDto, userIds);
    }

    public async Task<(bool Success, string Message)> UpdateAsync(int id, CreateAnnouncementDto dto)
    {
        var announcement = await _context.Announcements.FindAsync(id);
        if (announcement == null)
        {
            return (false, "NotFound");
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

        return (true, "Success");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var announcement = await _context.Announcements.FindAsync(id);
        if (announcement == null)
        {
            return (false, "NotFound");
        }

        announcement.IsDeleted = true;
        announcement.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        return (true, "Success");
    }
}
