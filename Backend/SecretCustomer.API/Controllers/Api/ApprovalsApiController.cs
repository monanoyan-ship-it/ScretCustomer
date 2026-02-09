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
public class ApprovalsApiController : BaseApiController
{
    private readonly ApplicationDbContext _context;
    private readonly ILocalizationService _localizationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IEvaluationService _evaluationService;

    public ApprovalsApiController(
        ApplicationDbContext context,
        ILocalizationService localizationService,
        IAuditLogService auditLogService,
        IEvaluationService evaluationService,
        IConfiguration configuration) : base(configuration)
    {
        _context = context;
        _localizationService = localizationService;
        _auditLogService = auditLogService;
        _evaluationService = evaluationService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Onayları listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetApprovals([FromQuery] ApprovalFilterDto filter)
    {
        var query = _context.Approvals
            .Include(a => a.RequestedByUser)
            .Include(a => a.RequestedByCustomerPersonnel)
            .Include(a => a.ApproverUser)
            .Include(a => a.ApprovedByUser)
            .AsQueryable();

        // Çoklu ApprovalTypes filtresi
        if (filter.ApprovalTypes?.Any() == true)
        {
            var typeIds = filter.ApprovalTypes
                .Select(t => ApprovalTypes.GetBySystemName(t)?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (typeIds.Any())
                query = query.Where(a => typeIds.Contains(a.ApprovalTypeId));
        }

        // Çoklu Statuses filtresi
        if (filter.Statuses?.Any() == true)
        {
            var statusIds = filter.Statuses
                .Select(s => ApprovalStatuses.GetBySystemName(s)?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (statusIds.Any())
                query = query.Where(a => statusIds.Contains(a.StatusId));
        }

        // Çoklu Priorities filtresi
        if (filter.Priorities?.Any() == true)
        {
            var priorityIds = filter.Priorities
                .Select(p => NotificationPriorities.GetBySystemName(p)?.Id)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (priorityIds.Any())
                query = query.Where(a => priorityIds.Contains(a.PriorityId));
        }

        // Çoklu RequestedByUserIds filtresi
        if (filter.RequestedByUserIds?.Any() == true)
            query = query.Where(a => a.RequestedByUserId.HasValue && filter.RequestedByUserIds.Contains(a.RequestedByUserId.Value));

        // Çoklu ApproverUserIds filtresi
        if (filter.ApproverUserIds?.Any() == true)
            query = query.Where(a => a.ApproverUserId.HasValue && filter.ApproverUserIds.Contains(a.ApproverUserId.Value));

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
                query = query.Where(a => a.RequestedAt >= minStart);
            if (maxEnd != DateTime.MaxValue)
                query = query.Where(a => a.RequestedAt <= maxEnd);
        }

        if (filter.IsOverdue.HasValue && filter.IsOverdue.Value)
            query = query.Where(a => a.DueDate.HasValue && a.DueDate.Value < DateTime.UtcNow && a.StatusId == ApprovalStatuses.Ids.Pending);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var searchTerm = filter.SearchTerm.ToLower();
            query = query.Where(a => a.Title.ToLower().Contains(searchTerm) ||
                                     a.ReferenceNumber.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();

        // Dynamic sorting
        var isAscending = filter.SortDirection?.ToLower() == "asc";
        IOrderedQueryable<Approval> orderedQuery = filter.SortBy?.ToLower() switch
        {
            "referencenumber" => isAscending ? query.OrderBy(a => a.ReferenceNumber) : query.OrderByDescending(a => a.ReferenceNumber),
            "approvaltype" => isAscending ? query.OrderBy(a => a.ApprovalTypeId) : query.OrderByDescending(a => a.ApprovalTypeId),
            "status" => isAscending ? query.OrderBy(a => a.StatusId) : query.OrderByDescending(a => a.StatusId),
            "title" => isAscending ? query.OrderBy(a => a.Title) : query.OrderByDescending(a => a.Title),
            "requestedbyusername" => isAscending
                ? query.OrderBy(a => a.RequestedByUser != null ? a.RequestedByUser.FirstName : a.RequestedByCustomerPersonnel != null ? a.RequestedByCustomerPersonnel.FirstName : "")
                : query.OrderByDescending(a => a.RequestedByUser != null ? a.RequestedByUser.FirstName : a.RequestedByCustomerPersonnel != null ? a.RequestedByCustomerPersonnel.FirstName : ""),
            "requestedat" => isAscending ? query.OrderBy(a => a.RequestedAt) : query.OrderByDescending(a => a.RequestedAt),
            "priority" => isAscending ? query.OrderBy(a => a.PriorityId) : query.OrderByDescending(a => a.PriorityId),
            _ => query.OrderByDescending(a => a.RequestedAt)
        };

        var approvals = await orderedQuery
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(a => new ApprovalListDto
            {
                Id = a.Id,
                ReferenceNumber = a.ReferenceNumber,
                ApprovalType = ApprovalTypes.GetById(a.ApprovalTypeId)!.SystemName,
                Status = ApprovalStatuses.GetById(a.StatusId)!.SystemName,
                Title = a.Title,
                RequestedByUserName = a.RequestedByUser != null
                        ? a.RequestedByUser.FirstName + " " + a.RequestedByUser.LastName
                        : a.RequestedByCustomerPersonnel != null
                            ? a.RequestedByCustomerPersonnel.FirstName + " " + a.RequestedByCustomerPersonnel.LastName
                            : "-",
                RequestedAt = a.RequestedAt,
                DueDate = a.DueDate,
                Priority = NotificationPriorities.GetById(a.PriorityId)!.SystemName,
                RelatedEntityId = a.RelatedEntityId,
                RelatedEntityType = a.RelatedEntityType
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
            .Include(a => a.RequestedByCustomerPersonnel)
            .Where(a => a.ApproverUserId == userId && a.StatusId == ApprovalStatuses.Ids.Pending)
            .OrderByDescending(a => a.PriorityId)
            .ThenBy(a => a.DueDate)
            .Select(a => new ApprovalListDto
            {
                Id = a.Id,
                ReferenceNumber = a.ReferenceNumber,
                ApprovalType = ApprovalTypes.GetById(a.ApprovalTypeId)!.SystemName,
                Status = ApprovalStatuses.GetById(a.StatusId)!.SystemName,
                Title = a.Title,
                RequestedByUserName = a.RequestedByUser != null
                        ? a.RequestedByUser.FirstName + " " + a.RequestedByUser.LastName
                        : a.RequestedByCustomerPersonnel != null
                            ? a.RequestedByCustomerPersonnel.FirstName + " " + a.RequestedByCustomerPersonnel.LastName
                            : "-",
                RequestedAt = a.RequestedAt,
                DueDate = a.DueDate,
                Priority = NotificationPriorities.GetById(a.PriorityId)!.SystemName,
                RelatedEntityId = a.RelatedEntityId,
                RelatedEntityType = a.RelatedEntityType
            })
            .ToListAsync();

        return Ok(approvals);
    }

    /// <summary>
    /// Onay detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetApproval(int id)
    {
        var approval = await _context.Approvals
            .Include(a => a.RequestedByUser)
            .Include(a => a.RequestedByCustomerPersonnel)
            .Include(a => a.ApproverUser)
            .Include(a => a.ApprovedByUser)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (approval == null)
            return NotFound();

        var dto = new ApprovalDto
        {
            Id = approval.Id,
            ReferenceNumber = approval.ReferenceNumber,
            ApprovalType = ApprovalTypes.GetById(approval.ApprovalTypeId)?.SystemName ?? "",
            Status = ApprovalStatuses.GetById(approval.StatusId)?.SystemName ?? "",
            Title = approval.Title,
            Description = approval.Description,
            RelatedEntityId = approval.RelatedEntityId,
            RelatedEntityType = approval.RelatedEntityType,
            RequestedByUserId = approval.RequestedByUserId,
            RequestedByUserName = approval.RequestedByUser != null
                ? $"{approval.RequestedByUser.FirstName} {approval.RequestedByUser.LastName}"
                : approval.RequestedByCustomerPersonnel != null
                    ? $"{approval.RequestedByCustomerPersonnel.FirstName} {approval.RequestedByCustomerPersonnel.LastName}"
                    : "-",
            ApproverUserId = approval.ApproverUserId,
            ApproverUserName = approval.ApproverUser != null ? $"{approval.ApproverUser.FirstName} {approval.ApproverUser.LastName}" : null,
            ApprovedByUserId = approval.ApprovedByUserId,
            ApprovedByUserName = approval.ApprovedByUser != null ? $"{approval.ApprovedByUser.FirstName} {approval.ApprovedByUser.LastName}" : null,
            RequestedAt = approval.RequestedAt,
            DueDate = approval.DueDate,
            RespondedAt = approval.RespondedAt,
            ResponseNote = approval.ResponseNote,
            Priority = NotificationPriorities.GetById(approval.PriorityId)?.SystemName ?? "",
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
        var currentUserId = GetCurrentUserId();
        var userType = User.FindFirst("UserType")?.Value;

        var approval = new Approval
        {
            ReferenceNumber = await GenerateApprovalNumber(),
            ApprovalTypeId = ApprovalTypes.GetBySystemName(dto.ApprovalType)?.Id ?? ApprovalTypes.Ids.General,
            StatusId = ApprovalStatuses.Ids.Pending,
            Title = dto.Title,
            Description = dto.Description,
            RelatedEntityId = dto.RelatedEntityId,
            RelatedEntityType = dto.RelatedEntityType,
            RequestedByUserId = userType == "CustomerPersonnel" ? null : currentUserId,
            RequestedByCustomerPersonnelId = userType == "CustomerPersonnel" ? currentUserId : null,
            ApproverUserId = dto.ApproverUserId,
            RequestedAt = DateTime.UtcNow,
            DueDate = dto.DueDate,
            PriorityId = NotificationPriorities.GetBySystemName(dto.Priority)?.Id ?? NotificationPriorities.Ids.Normal,
            AutoApproveHours = dto.AutoApproveHours,
            RequiredApprovalLevels = dto.RequiredApprovalLevels
        };

        _context.Approvals.Add(approval);

        // Create notification for approver
        if (dto.ApproverUserId.HasValue)
        {
            var notification = new Notification
            {
                NotificationTypeId = NotificationTypes.Ids.ApprovalRequest,
                ChannelId = NotificationChannels.Ids.InApp,
                PriorityId = approval.PriorityId,
                Title = "Yeni Onay Talebi",
                Message = $"{approval.Title} için onay talebiniz var.",
                RecipientUserId = dto.ApproverUserId.Value,
                SenderUserId = userType == "CustomerPersonnel" ? null : currentUserId,
                RelatedEntityId = approval.Id,
                RelatedEntityType = "Approval",
                ActionUrl = $"/Approvals/Detail/{approval.Id}"
            };
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();

        // Audit Log
        await _auditLogService.LogInfoAsync(
            $"Onay talebi oluşturuldu: {approval.ReferenceNumber} - {approval.Title}",
            "ApprovalService");

        return Ok(new { id = approval.Id, referenceNumber = approval.ReferenceNumber });
    }

    /// <summary>
    /// Onay yanıtı ver
    /// </summary>
    [HttpPost("{id}/respond")]
    public async Task<IActionResult> RespondToApproval(int id, [FromBody] ApprovalResponseDto dto)
    {
        var approval = await _context.Approvals.FindAsync(id);
        if (approval == null)
            return NotFound();

        if (approval.StatusId != ApprovalStatuses.Ids.Pending)
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Approval.AlreadyResponded")));

        var userId = GetCurrentUserId();

        approval.StatusId = dto.Approved ? ApprovalStatuses.Ids.Approved : ApprovalStatuses.Ids.Rejected;
        approval.ApprovedByUserId = userId;
        approval.RespondedAt = DateTime.UtcNow;
        approval.ResponseNote = dto.Note;

        // === HOOK: Taslağa Alma Talebi Onayı ===
        // Eğer bu bir taslağa alma talebi ise ve onaylandıysa, değerlendirmeyi taslağa al
        if (dto.Approved &&
            approval.ApprovalTypeId == ApprovalTypes.Ids.Evaluation &&
            approval.RelatedEntityType == "EvaluationRevert" &&
            approval.RelatedEntityId.HasValue)
        {
            try
            {
                await _evaluationService.RevertToDraftAsync(
                    approval.RelatedEntityId.Value,
                    userId,
                    $"Taslağa alma talebi onaylandı. Talep No: {approval.ReferenceNumber}. Neden: {approval.Description}");
            }
            catch (Exception ex)
            {
                // Taslağa alma başarısız olursa approval'ı geri al
                approval.StatusId = ApprovalStatuses.Ids.Pending;
                approval.ApprovedByUserId = null;
                approval.RespondedAt = null;
                approval.ResponseNote = null;
                await _context.SaveChangesAsync();

                return BadRequest(CreateErrorResponse($"Değerlendirme taslağa alınamadı: {ex.Message}"));
            }
        }

        // Notify requester (only if requester is a User, not CustomerPersonnel)
        if (approval.RequestedByUserId.HasValue)
        {
            var notification = new Notification
            {
                NotificationTypeId = dto.Approved ? NotificationTypes.Ids.Success : NotificationTypes.Ids.Warning,
                ChannelId = NotificationChannels.Ids.InApp,
                PriorityId = NotificationPriorities.Ids.Normal,
                Title = dto.Approved ? "Onay Kabul Edildi" : "Onay Reddedildi",
                Message = $"{approval.Title} için onay talebiniz {(dto.Approved ? "kabul edildi" : "reddedildi")}.",
                RecipientUserId = approval.RequestedByUserId.Value,
                SenderUserId = userId,
                RelatedEntityId = approval.Id,
                RelatedEntityType = "Approval",
                ActionUrl = $"/Approvals/Detail/{approval.Id}"
            };
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();

        // Audit Log
        var logMessage = dto.Approved
            ? $"Onay kabul edildi: {approval.ReferenceNumber} - {approval.Title}"
            : $"Onay reddedildi: {approval.ReferenceNumber} - {approval.Title}";
        await _auditLogService.LogInfoAsync(logMessage, "ApprovalService");

        return Ok();
    }

    /// <summary>
    /// Onay talebini iptal et
    /// </summary>
    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelApproval(int id)
    {
        var approval = await _context.Approvals.FindAsync(id);
        if (approval == null)
            return NotFound();

        var cancelUserId = GetCurrentUserId();
        var cancelUserType = User.FindFirst("UserType")?.Value;
        var isOwner = cancelUserType == "CustomerPersonnel"
            ? approval.RequestedByCustomerPersonnelId == cancelUserId
            : approval.RequestedByUserId == cancelUserId;
        if (!isOwner)
            return Forbid();

        if (approval.StatusId != ApprovalStatuses.Ids.Pending)
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Approval.CannotCancel")));

        approval.StatusId = ApprovalStatuses.Ids.Cancelled;
        await _context.SaveChangesAsync();

        // Audit Log
        await _auditLogService.LogInfoAsync(
            $"Onay talebi iptal edildi: {approval.ReferenceNumber} - {approval.Title}",
            "ApprovalService");

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
            PendingApprovals = await _context.Approvals.CountAsync(a => a.StatusId == ApprovalStatuses.Ids.Pending),
            ApprovedCount = await _context.Approvals.CountAsync(a => a.StatusId == ApprovalStatuses.Ids.Approved),
            RejectedCount = await _context.Approvals.CountAsync(a => a.StatusId == ApprovalStatuses.Ids.Rejected),
            OverdueCount = await _context.Approvals.CountAsync(a => a.StatusId == ApprovalStatuses.Ids.Pending && a.DueDate.HasValue && a.DueDate.Value < now),
            TodayApprovals = await _context.Approvals.CountAsync(a => a.RequestedAt.Date == today),
            RecentApprovals = await _context.Approvals
                .Include(a => a.RequestedByUser)
                .Include(a => a.RequestedByCustomerPersonnel)
                .OrderByDescending(a => a.RequestedAt)
                .Take(5)
                .Select(a => new ApprovalListDto
                {
                    Id = a.Id,
                    ReferenceNumber = a.ReferenceNumber,
                    ApprovalType = ApprovalTypes.GetById(a.ApprovalTypeId)!.SystemName,
                    Status = ApprovalStatuses.GetById(a.StatusId)!.SystemName,
                    Title = a.Title,
                    RequestedByUserName = a.RequestedByUser != null
                        ? a.RequestedByUser.FirstName + " " + a.RequestedByUser.LastName
                        : a.RequestedByCustomerPersonnel != null
                            ? a.RequestedByCustomerPersonnel.FirstName + " " + a.RequestedByCustomerPersonnel.LastName
                            : "-",
                    RequestedAt = a.RequestedAt,
                    DueDate = a.DueDate,
                    Priority = NotificationPriorities.GetById(a.PriorityId)!.SystemName,
                    RelatedEntityId = a.RelatedEntityId,
                    RelatedEntityType = a.RelatedEntityType
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
