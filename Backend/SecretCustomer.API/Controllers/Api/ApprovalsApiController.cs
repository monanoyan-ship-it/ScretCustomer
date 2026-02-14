using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Approval;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.API.Controllers.Api;

/// <summary>
/// Onay yönetimi API controller
/// </summary>
[Route("api/approvals")]
[ApiController]
[Authorize]
public class ApprovalsApiController : BaseApiController
{
    private readonly IApprovalService _approvalService;
    private readonly ILocalizationService _localizationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IEvaluationService _evaluationService;
    private readonly INotificationCreatorService _notificationCreator;

    public ApprovalsApiController(
        IApprovalService approvalService,
        ILocalizationService localizationService,
        IAuditLogService auditLogService,
        IEvaluationService evaluationService,
        INotificationCreatorService notificationCreator,
        IConfiguration configuration) : base(configuration)
    {
        _approvalService = approvalService;
        _localizationService = localizationService;
        _auditLogService = auditLogService;
        _evaluationService = evaluationService;
        _notificationCreator = notificationCreator;
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
        var (items, totalCount) = await _approvalService.GetApprovalsAsync(filter);
        return Ok(new { items, totalCount });
    }

    /// <summary>
    /// Benim onaylarım
    /// </summary>
    [HttpGet("my-pending")]
    public async Task<IActionResult> GetMyPendingApprovals()
    {
        var userId = GetCurrentUserId();
        var approvals = await _approvalService.GetMyPendingApprovalsAsync(userId);
        return Ok(approvals);
    }

    /// <summary>
    /// Onay detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetApproval(int id)
    {
        var dto = await _approvalService.GetApprovalAsync(id);
        if (dto == null)
            return NotFound();

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
            ReferenceNumber = await _approvalService.GenerateApprovalNumberAsync(),
            ApprovalTypeId = ApprovalTypes.GetBySystemName(dto.ApprovalType)?.Id ?? ApprovalTypes.Ids.General,
            StatusId = ApprovalStatuses.Ids.Pending,
            Title = dto.Title,
            Description = dto.Description,
            RelatedEntityId = dto.RelatedEntityId,
            RelatedEntityType = dto.RelatedEntityType,
            RequestedByUserId = userType == "CustomerPersonnel" ? null : currentUserId,
            RequestedByCustomerPersonnelId = userType == "CustomerPersonnel" ? currentUserId : null,
            ApproverUserId = dto.ApproverUserId,
            RequestedAt = TurkeyTime.Now,
            DueDate = dto.DueDate,
            PriorityId = NotificationPriorities.GetBySystemName(dto.Priority)?.Id ?? NotificationPriorities.Ids.Normal,
            AutoApproveHours = dto.AutoApproveHours,
            RequiredApprovalLevels = dto.RequiredApprovalLevels
        };

        var (id, referenceNumber) = await _approvalService.CreateApprovalAsync(approval);

        // Create notification for approver via service (SignalR push + email)
        if (dto.ApproverUserId.HasValue)
        {
            await _notificationCreator.CreateAsync(
                dto.ApproverUserId.Value,
                NotificationTypes.Ids.ApprovalRequest,
                "Yeni Onay Talebi",
                $"{approval.Title} için onay talebiniz var.",
                actionUrl: "/Evaluations",
                relatedEntityId: approval.Id,
                relatedEntityType: "Approval",
                priorityId: approval.PriorityId,
                senderUserId: userType == "CustomerPersonnel" ? null : currentUserId);
        }

        // Audit Log
        await _auditLogService.LogInfoAsync(
            $"Onay talebi oluşturuldu: {approval.ReferenceNumber} - {approval.Title}",
            "ApprovalService");

        return Ok(new { id, referenceNumber });
    }

    /// <summary>
    /// Onay yanıtı ver
    /// </summary>
    [HttpPost("{id}/respond")]
    public async Task<IActionResult> RespondToApproval(int id, [FromBody] ApprovalResponseDto dto)
    {
        var approval = await _approvalService.FindByIdAsync(id);
        if (approval == null)
            return NotFound();

        if (approval.StatusId != ApprovalStatuses.Ids.Pending)
            return BadRequest(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Approval.AlreadyResponded")));

        var userId = GetCurrentUserId();

        approval.StatusId = dto.Approved ? ApprovalStatuses.Ids.Approved : ApprovalStatuses.Ids.Rejected;
        approval.ApprovedByUserId = userId;
        approval.RespondedAt = TurkeyTime.Now;
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
                await _approvalService.SaveChangesAsync();

                return BadRequest(CreateErrorResponse($"Değerlendirme taslağa alınamadı: {ex.Message}"));
            }
        }

        await _approvalService.SaveChangesAsync();

        // Notify requester via service (SignalR push + email)
        if (approval.RequestedByUserId.HasValue)
        {
            await _notificationCreator.CreateAsync(
                approval.RequestedByUserId.Value,
                dto.Approved ? NotificationTypes.Ids.Success : NotificationTypes.Ids.Warning,
                dto.Approved ? "Onay Kabul Edildi" : "Onay Reddedildi",
                $"{approval.Title} için onay talebiniz {(dto.Approved ? "kabul edildi" : "reddedildi")}.",
                actionUrl: "/Evaluations",
                relatedEntityId: approval.Id,
                relatedEntityType: "Approval",
                senderUserId: userId);
        }

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
        var approval = await _approvalService.FindByIdAsync(id);
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
        await _approvalService.SaveChangesAsync();

        // Onaylayıcıya bildirim gönder
        if (approval.ApproverUserId.HasValue)
        {
            await _notificationCreator.CreateAsync(
                approval.ApproverUserId.Value,
                NotificationTypes.Ids.Warning,
                "Onay Talebi İptal Edildi",
                $"{approval.Title} onay talebi iptal edildi.",
                actionUrl: "/Evaluations",
                relatedEntityId: approval.Id,
                relatedEntityType: "Approval",
                senderUserId: cancelUserType == "CustomerPersonnel" ? null : cancelUserId);
        }

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
        var summary = await _approvalService.GetSummaryAsync();
        return Ok(summary);
    }
}
