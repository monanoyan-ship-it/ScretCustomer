using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Approval;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

/// <summary>
/// Onay yönetimi API controller
/// </summary>
[Route("api/approvals")]
[ApiController]
[Authorize]
public class ApprovalsApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILocalizationService _localizationService;

    public ApprovalsApiController(ApplicationDbContext context, ILocalizationService localizationService)
    {
        _context = context;
        _localizationService = localizationService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
    }

    /// <summary>
    /// Onayları listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetApprovals([FromQuery] ApprovalFilterDto filter)
    {
        var query = _context.Approvals
            .Include(a => a.RequestedByUser)
            .Include(a => a.ApproverUser)
            .Include(a => a.ApprovedByUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.ApprovalType) && Enum.TryParse<ApprovalType>(filter.ApprovalType, out var approvalType))
            query = query.Where(a => a.ApprovalType == approvalType);

        if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<ApprovalStatus>(filter.Status, out var status))
            query = query.Where(a => a.Status == status);

        if (!string.IsNullOrEmpty(filter.Priority) && Enum.TryParse<NotificationPriority>(filter.Priority, out var priority))
            query = query.Where(a => a.Priority == priority);

        if (filter.RequestedByUserId.HasValue)
            query = query.Where(a => a.RequestedByUserId == filter.RequestedByUserId.Value);

        if (filter.ApproverUserId.HasValue)
            query = query.Where(a => a.ApproverUserId == filter.ApproverUserId.Value);

        if (filter.StartDate.HasValue)
            query = query.Where(a => a.RequestedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(a => a.RequestedAt <= filter.EndDate.Value);

        if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
            query = query.Where(a => a.DueDate.HasValue && a.DueDate.Value < DateTime.UtcNow && a.Status == ApprovalStatus.Pending);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(a => a.Title.ToLower().Contains(searchTerm) ||
                                     a.ReferenceNumber.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        var approvals = await query
            .OrderByDescending(a => a.RequestedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new ApprovalListDto
            {
                Id = a.Id,
                ReferenceNumber = a.ReferenceNumber,
                ApprovalType = a.ApprovalType.ToString(),
                Status = a.Status.ToString(),
                Title = a.Title,
                RequestedByUserName = a.RequestedByUser.FirstName + " " + a.RequestedByUser.LastName,
                RequestedAt = a.RequestedAt,
                DueDate = a.DueDate,
                Priority = a.Priority.ToString()
            })
            .ToListAsync();

        return Ok(new { items = approvals, totalCount });
    }

    /// <summary>
    /// Benim onaylarım
    /// </summary>
    [HttpGet("my-pending")]
    public async Task<IActionResult> GetMyPendingApprovals()
    {
        var userId = GetCurrentUserId();

        var approvals = await _context.Approvals
            .Include(a => a.RequestedByUser)
            .Where(a => a.ApproverUserId == userId && a.Status == ApprovalStatus.Pending)
            .OrderByDescending(a => a.Priority)
            .ThenBy(a => a.DueDate)
            .Select(a => new ApprovalListDto
            {
                Id = a.Id,
                ReferenceNumber = a.ReferenceNumber,
                ApprovalType = a.ApprovalType.ToString(),
                Status = a.Status.ToString(),
                Title = a.Title,
                RequestedByUserName = a.RequestedByUser.FirstName + " " + a.RequestedByUser.LastName,
                RequestedAt = a.RequestedAt,
                DueDate = a.DueDate,
                Priority = a.Priority.ToString()
            })
            .ToListAsync();

        return Ok(approvals);
    }

    /// <summary>
    /// Onay detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetApproval(Guid id)
    {
        var approval = await _context.Approvals
            .Include(a => a.RequestedByUser)
            .Include(a => a.ApproverUser)
            .Include(a => a.ApprovedByUser)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (approval == null)
            return NotFound();

        var dto = new ApprovalDto
        {
            Id = approval.Id,
            ReferenceNumber = approval.ReferenceNumber,
            ApprovalType = approval.ApprovalType.ToString(),
            Status = approval.Status.ToString(),
            Title = approval.Title,
            Description = approval.Description,
            RelatedEntityId = approval.RelatedEntityId,
            RelatedEntityType = approval.RelatedEntityType,
            RequestedByUserId = approval.RequestedByUserId,
            RequestedByUserName = $"{approval.RequestedByUser.FirstName} {approval.RequestedByUser.LastName}",
            ApproverUserId = approval.ApproverUserId,
            ApproverUserName = approval.ApproverUser != null ? $"{approval.ApproverUser.FirstName} {approval.ApproverUser.LastName}" : null,
            ApprovedByUserId = approval.ApprovedByUserId,
            ApprovedByUserName = approval.ApprovedByUser != null ? $"{approval.ApprovedByUser.FirstName} {approval.ApprovedByUser.LastName}" : null,
            RequestedAt = approval.RequestedAt,
            DueDate = approval.DueDate,
            RespondedAt = approval.RespondedAt,
            ResponseNote = approval.ResponseNote,
            Priority = approval.Priority.ToString(),
            ApprovalLevel = approval.ApprovalLevel,
            RequiredApprovalLevels = approval.RequiredApprovalLevels,
            CreatedAt = approval.CreatedAt
        };

        return Ok(dto);
    }

    /// <summary>
    /// Onay talebi oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateApproval([FromBody] CreateApprovalDto dto)
    {
        var approval = new Approval
        {
            ReferenceNumber = await GenerateApprovalNumber(),
            ApprovalType = Enum.TryParse<ApprovalType>(dto.ApprovalType, out var approvalType) ? approvalType : ApprovalType.General,
            Status = ApprovalStatus.Pending,
            Title = dto.Title,
            Description = dto.Description,
            RelatedEntityId = dto.RelatedEntityId,
            RelatedEntityType = dto.RelatedEntityType,
            RequestedByUserId = GetCurrentUserId(),
            ApproverUserId = dto.ApproverUserId,
            RequestedAt = DateTime.UtcNow,
            DueDate = dto.DueDate,
            Priority = Enum.TryParse<NotificationPriority>(dto.Priority, out var priority) ? priority : NotificationPriority.Normal,
            AutoApproveHours = dto.AutoApproveHours,
            RequiredApprovalLevels = dto.RequiredApprovalLevels
        };

        _context.Approvals.Add(approval);

        // Create notification for approver
        if (dto.ApproverUserId.HasValue)
        {
            var notification = new Notification
            {
                NotificationType = NotificationType.ApprovalRequest,
                Channel = NotificationChannel.InApp,
                Priority = approval.Priority,
                Title = "Yeni Onay Talebi",
                Message = $"{approval.Title} için onay talebiniz var.",
                RecipientUserId = dto.ApproverUserId.Value,
                SenderUserId = GetCurrentUserId(),
                RelatedEntityId = approval.Id,
                RelatedEntityType = "Approval",
                ActionUrl = $"/Approvals/Detail/{approval.Id}"
            };
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();

        return Ok(new { id = approval.Id, referenceNumber = approval.ReferenceNumber });
    }

    /// <summary>
    /// Onay yanıtı ver
    /// </summary>
    [HttpPost("{id}/respond")]
    public async Task<IActionResult> RespondToApproval(Guid id, [FromBody] ApprovalResponseDto dto)
    {
        var approval = await _context.Approvals.FindAsync(id);
        if (approval == null)
            return NotFound();

        if (approval.Status != ApprovalStatus.Pending)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.Approval.AlreadyResponded") });

        var userId = GetCurrentUserId();

        approval.Status = dto.Approved ? ApprovalStatus.Approved : ApprovalStatus.Rejected;
        approval.ApprovedByUserId = userId;
        approval.RespondedAt = DateTime.UtcNow;
        approval.ResponseNote = dto.Note;

        // Notify requester
        var notification = new Notification
        {
            NotificationType = dto.Approved ? NotificationType.Success : NotificationType.Warning,
            Channel = NotificationChannel.InApp,
            Priority = NotificationPriority.Normal,
            Title = dto.Approved ? "Onay Kabul Edildi" : "Onay Reddedildi",
            Message = $"{approval.Title} için onay talebiniz {(dto.Approved ? "kabul edildi" : "reddedildi")}.",
            RecipientUserId = approval.RequestedByUserId,
            SenderUserId = userId,
            RelatedEntityId = approval.Id,
            RelatedEntityType = "Approval",
            ActionUrl = $"/Approvals/Detail/{approval.Id}"
        };
        _context.Notifications.Add(notification);

        await _context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Onay talebini iptal et
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelApproval(Guid id)
    {
        var approval = await _context.Approvals.FindAsync(id);
        if (approval == null)
            return NotFound();

        if (approval.RequestedByUserId != GetCurrentUserId())
            return Forbid();

        if (approval.Status != ApprovalStatus.Pending)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.Approval.CannotCancel") });

        approval.Status = ApprovalStatus.Cancelled;
        await _context.SaveChangesAsync();

        return Ok();
    }

    /// <summary>
    /// Onay özeti
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var userId = GetCurrentUserId();
        var now = DateTime.UtcNow;
        var today = now.Date;

        var summary = new ApprovalSummaryDto
        {
            TotalApprovals = await _context.Approvals.CountAsync(),
            PendingApprovals = await _context.Approvals.CountAsync(a => a.Status == ApprovalStatus.Pending),
            ApprovedCount = await _context.Approvals.CountAsync(a => a.Status == ApprovalStatus.Approved),
            RejectedCount = await _context.Approvals.CountAsync(a => a.Status == ApprovalStatus.Rejected),
            OverdueCount = await _context.Approvals.CountAsync(a => a.Status == ApprovalStatus.Pending && a.DueDate.HasValue && a.DueDate.Value < now),
            TodayApprovals = await _context.Approvals.CountAsync(a => a.RequestedAt.Date == today),
            RecentApprovals = await _context.Approvals
                .Include(a => a.RequestedByUser)
                .OrderByDescending(a => a.RequestedAt)
                .Take(5)
                .Select(a => new ApprovalListDto
                {
                    Id = a.Id,
                    ReferenceNumber = a.ReferenceNumber,
                    ApprovalType = a.ApprovalType.ToString(),
                    Status = a.Status.ToString(),
                    Title = a.Title,
                    RequestedByUserName = a.RequestedByUser.FirstName + " " + a.RequestedByUser.LastName,
                    RequestedAt = a.RequestedAt,
                    DueDate = a.DueDate,
                    Priority = a.Priority.ToString()
                })
                .ToListAsync()
        };

        return Ok(summary);
    }

    private async Task<string> GenerateApprovalNumber()
    {
        var year = DateTime.UtcNow.Year;
        var count = await _context.Approvals.CountAsync(a => a.CreatedAt.Year == year) + 1;
        return $"APR-{year}-{count:D4}";
    }
}
