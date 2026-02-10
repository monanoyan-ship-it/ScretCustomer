using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SecretCustomer.Core.DTOs.SupportRequest;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class SupportRequestService : ISupportRequestService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SupportRequestService> _logger;
    private readonly INotificationCreatorService _notificationCreator;

    public SupportRequestService(
        ApplicationDbContext context,
        ILogger<SupportRequestService> logger,
        INotificationCreatorService notificationCreator)
    {
        _context = context;
        _logger = logger;
        _notificationCreator = notificationCreator;
    }

    public async Task<(List<SupportRequestListDto> Items, int TotalCount)> GetAllAsync(SupportRequestFilterDto filter)
    {
        var query = _context.SupportRequests
            .Include(sr => sr.User)
            .Include(sr => sr.CustomerPersonnel)
            .Include(sr => sr.RespondedByUser)
            .AsQueryable();

        // Filtreler
        if (filter.StatusId.HasValue)
        {
            query = query.Where(sr => sr.StatusId == filter.StatusId.Value);
        }

        if (!string.IsNullOrEmpty(filter.SenderType))
        {
            if (filter.SenderType == "User")
                query = query.Where(sr => sr.UserId != null);
            else if (filter.SenderType == "CustomerPersonnel")
                query = query.Where(sr => sr.CustomerPersonnelId != null);
        }

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var term = filter.SearchTerm.ToLower();
            query = query.Where(sr =>
                sr.Message.ToLower().Contains(term) ||
                (sr.User != null && (sr.User.FirstName.ToLower().Contains(term) || sr.User.LastName.ToLower().Contains(term))) ||
                (sr.CustomerPersonnel != null && (sr.CustomerPersonnel.FirstName.ToLower().Contains(term) || sr.CustomerPersonnel.LastName.ToLower().Contains(term))));
        }

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
                query = query.Where(sr => sr.CreatedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(sr => sr.CreatedAt <= maxEnd);
        }

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(sr => sr.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(sr => MapToListDto(sr))
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<SupportRequestDetailDto?> GetByIdAsync(int id)
    {
        var request = await _context.SupportRequests
            .Include(sr => sr.User)
            .Include(sr => sr.CustomerPersonnel)
            .Include(sr => sr.RespondedByUser)
            .FirstOrDefaultAsync(sr => sr.Id == id);

        return request == null ? null : MapToDetailDto(request);
    }

    public async Task<List<SupportRequestListDto>> GetByUserAsync(int? userId, int? customerPersonnelId)
    {
        var query = _context.SupportRequests.AsQueryable();

        if (userId.HasValue)
            query = query.Where(sr => sr.UserId == userId.Value);
        else if (customerPersonnelId.HasValue)
            query = query.Where(sr => sr.CustomerPersonnelId == customerPersonnelId.Value);
        else
            return new List<SupportRequestListDto>();

        return await query
            .OrderByDescending(sr => sr.CreatedAt)
            .Take(50)
            .Select(sr => MapToListDto(sr))
            .ToListAsync();
    }

    public async Task<MySupportRequestSummaryDto> GetMySummaryAsync(int? userId, int? customerPersonnelId)
    {
        var query = _context.SupportRequests.AsQueryable();

        if (userId.HasValue)
            query = query.Where(sr => sr.UserId == userId.Value);
        else if (customerPersonnelId.HasValue)
            query = query.Where(sr => sr.CustomerPersonnelId == customerPersonnelId.Value);
        else
            return new MySupportRequestSummaryDto();

        var totalRequests = await query.CountAsync();
        var pendingCount = await query.CountAsync(sr => sr.StatusId == SupportRequestStatuses.Ids.Pending || sr.StatusId == SupportRequestStatuses.Ids.InProgress);
        var unreadResponseCount = await query.CountAsync(sr => sr.AdminResponse != null && !sr.IsReadByUser);

        var recentRequests = await query
            .OrderByDescending(sr => sr.CreatedAt)
            .Take(5)
            .Select(sr => MapToListDto(sr))
            .ToListAsync();

        return new MySupportRequestSummaryDto
        {
            TotalRequests = totalRequests,
            PendingCount = pendingCount,
            UnreadResponseCount = unreadResponseCount,
            RecentRequests = recentRequests
        };
    }

    public async Task<SupportRequestDetailDto> CreateAsync(CreateSupportRequestDto dto, int? userId, int? customerPersonnelId)
    {
        if (!userId.HasValue && !customerPersonnelId.HasValue)
            throw new InvalidOperationException("Either userId or customerPersonnelId must be provided");

        var request = new SupportRequest
        {
            UserId = userId,
            CustomerPersonnelId = customerPersonnelId,
            Message = dto.Message,
            PageUrl = dto.PageUrl,
            StatusId = SupportRequestStatuses.Ids.Pending
        };

        _context.SupportRequests.Add(request);
        await _context.SaveChangesAsync();

        // Admin'lere bildirim gönder (SignalR push + email)
        var adminIds = await _context.Users
            .Where(u => u.RoleId == UserRoles.Ids.Admin && u.IsActive && !u.IsDeleted)
            .Select(u => u.Id)
            .ToListAsync();

        string senderName = userId.HasValue
            ? await _context.Users.Where(u => u.Id == userId).Select(u => u.FirstName + " " + u.LastName).FirstOrDefaultAsync() ?? "Kullanıcı"
            : await _context.CustomerPersonnel.Where(cp => cp.Id == customerPersonnelId).Select(cp => cp.FirstName + " " + cp.LastName).FirstOrDefaultAsync() ?? "Müşteri Personeli";

        if (adminIds.Any())
        {
            await _notificationCreator.CreateBulkAsync(
                adminIds,
                NotificationTypes.Ids.Info,
                "Yeni Destek Talebi",
                $"Yeni destek talebi: {senderName}",
                actionUrl: $"/SupportRequests?id={request.Id}",
                relatedEntityId: request.Id,
                relatedEntityType: "SupportRequest",
                senderUserId: userId);
        }

        _logger.LogInformation("Support request created: {Id} by User: {UserId} / CustomerPersonnel: {CustomerPersonnelId}",
            request.Id, userId, customerPersonnelId);

        return await GetByIdAsync(request.Id) ?? throw new Exception("Request not found after creation");
    }

    public async Task<SupportRequestDetailDto> RespondAsync(RespondSupportRequestDto dto, int adminUserId)
    {
        var request = await _context.SupportRequests.FirstOrDefaultAsync(sr => sr.Id == dto.Id);

        if (request == null)
            throw new KeyNotFoundException($"Support request not found: {dto.Id}");

        request.AdminResponse = dto.AdminResponse;
        request.RespondedByUserId = adminUserId;
        request.RespondedAt = DateTime.UtcNow;
        request.StatusId = SupportRequestStatuses.Ids.Resolved;
        request.IsReadByUser = false; // Cevap geldi, kullanıcı henüz okumadı

        await _context.SaveChangesAsync();

        // Talep sahibine bildirim gönder (SignalR push + email)
        if (request.UserId.HasValue)
        {
            await _notificationCreator.CreateAsync(
                request.UserId.Value,
                NotificationTypes.Ids.Success,
                "Destek Talebi Cevaplandı",
                "Destek talebiniz cevaplandı.",
                actionUrl: "/MySupport",
                relatedEntityId: request.Id,
                relatedEntityType: "SupportRequest",
                senderUserId: adminUserId);
        }
        else if (request.CustomerPersonnelId.HasValue)
        {
            // CustomerPersonnel için bildirim - Notification tablosu sadece User destekler
            _logger.LogInformation("Support request {Id} responded. CustomerPersonnel {PersonnelId} should be notified.",
                request.Id, request.CustomerPersonnelId);
        }

        _logger.LogInformation("Support request responded: {Id} by Admin: {AdminUserId}", request.Id, adminUserId);

        return await GetByIdAsync(request.Id) ?? throw new Exception("Request not found after response");
    }

    public async Task<SupportRequestDetailDto> UpdateStatusAsync(UpdateSupportRequestStatusDto dto)
    {
        var request = await _context.SupportRequests.FirstOrDefaultAsync(sr => sr.Id == dto.Id);

        if (request == null)
            throw new KeyNotFoundException($"Support request not found: {dto.Id}");

        request.StatusId = dto.StatusId;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Support request status updated: {Id} to {StatusId}", request.Id, dto.StatusId);

        return await GetByIdAsync(request.Id) ?? throw new Exception("Request not found after status update");
    }

    public async Task MarkAsReadAsync(int id, int? userId, int? customerPersonnelId)
    {
        var request = await _context.SupportRequests.FirstOrDefaultAsync(sr => sr.Id == id);

        if (request == null)
            throw new KeyNotFoundException($"Support request not found: {id}");

        // Sadece talep sahibi okuyabilir
        if ((userId.HasValue && request.UserId == userId) ||
            (customerPersonnelId.HasValue && request.CustomerPersonnelId == customerPersonnelId))
        {
            request.IsReadByUser = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int id)
    {
        var request = await _context.SupportRequests.FirstOrDefaultAsync(sr => sr.Id == id);

        if (request == null)
            throw new KeyNotFoundException($"Support request not found: {id}");

        request.IsDeleted = true;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Support request deleted: {Id}", id);
    }

    public async Task<(int Pending, int InProgress, int Resolved, int Closed)> GetStatusCountsAsync()
    {
        var counts = await _context.SupportRequests
            .GroupBy(sr => sr.StatusId)
            .Select(g => new { StatusId = g.Key, Count = g.Count() })
            .ToListAsync();

        return (
            counts.FirstOrDefault(c => c.StatusId == SupportRequestStatuses.Ids.Pending)?.Count ?? 0,
            counts.FirstOrDefault(c => c.StatusId == SupportRequestStatuses.Ids.InProgress)?.Count ?? 0,
            counts.FirstOrDefault(c => c.StatusId == SupportRequestStatuses.Ids.Resolved)?.Count ?? 0,
            counts.FirstOrDefault(c => c.StatusId == SupportRequestStatuses.Ids.Closed)?.Count ?? 0
        );
    }

    private static SupportRequestListDto MapToListDto(SupportRequest sr)
    {
        var status = SupportRequestStatuses.GetById(sr.StatusId);
        string senderName;
        string senderType;

        if (sr.User != null)
        {
            senderName = $"{sr.User.FirstName} {sr.User.LastName}";
            senderType = "User";
        }
        else if (sr.CustomerPersonnel != null)
        {
            senderName = $"{sr.CustomerPersonnel.FirstName} {sr.CustomerPersonnel.LastName}";
            senderType = "CustomerPersonnel";
        }
        else
        {
            senderName = "Bilinmiyor";
            senderType = "Unknown";
        }

        return new SupportRequestListDto
        {
            Id = sr.Id,
            Message = sr.Message.Length > 100 ? sr.Message.Substring(0, 100) + "..." : sr.Message,
            PageUrl = sr.PageUrl,
            StatusId = sr.StatusId,
            StatusName = status?.NameResourceKey ?? "Unknown",
            StatusIcon = status?.Icon ?? "",
            StatusCssClass = status?.CssClass ?? "",
            HasResponse = !string.IsNullOrEmpty(sr.AdminResponse),
            CreatedAt = sr.CreatedAt,
            RespondedAt = sr.RespondedAt,
            SenderName = senderName,
            SenderType = senderType,
            UserId = sr.UserId,
            CustomerPersonnelId = sr.CustomerPersonnelId
        };
    }

    private static SupportRequestDetailDto MapToDetailDto(SupportRequest sr)
    {
        var status = SupportRequestStatuses.GetById(sr.StatusId);
        string senderName;
        string? senderEmail;
        string senderType;

        if (sr.User != null)
        {
            senderName = $"{sr.User.FirstName} {sr.User.LastName}";
            senderEmail = sr.User.Email;
            senderType = "User";
        }
        else if (sr.CustomerPersonnel != null)
        {
            senderName = $"{sr.CustomerPersonnel.FirstName} {sr.CustomerPersonnel.LastName}";
            senderEmail = sr.CustomerPersonnel.Email;
            senderType = "CustomerPersonnel";
        }
        else
        {
            senderName = "Bilinmiyor";
            senderEmail = null;
            senderType = "Unknown";
        }

        return new SupportRequestDetailDto
        {
            Id = sr.Id,
            Message = sr.Message,
            PageUrl = sr.PageUrl,
            StatusId = sr.StatusId,
            StatusName = status?.NameResourceKey ?? "Unknown",
            StatusIcon = status?.Icon ?? "",
            StatusCssClass = status?.CssClass ?? "",
            AdminResponse = sr.AdminResponse,
            RespondedAt = sr.RespondedAt,
            IsReadByUser = sr.IsReadByUser,
            CreatedAt = sr.CreatedAt,
            SenderName = senderName,
            SenderEmail = senderEmail,
            SenderType = senderType,
            UserId = sr.UserId,
            CustomerPersonnelId = sr.CustomerPersonnelId,
            RespondedByUserName = sr.RespondedByUser != null
                ? $"{sr.RespondedByUser.FirstName} {sr.RespondedByUser.LastName}"
                : null
        };
    }
}
