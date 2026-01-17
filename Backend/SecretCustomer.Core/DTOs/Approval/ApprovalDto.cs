namespace SecretCustomer.Core.DTOs.Approval;

/// <summary>
/// Onay DTO
/// </summary>
public class ApprovalDto
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string ApprovalType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public int RequestedByUserId { get; set; }
    public string? RequestedByUserName { get; set; }
    public int? ApproverUserId { get; set; }
    public string? ApproverUserName { get; set; }
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? RespondedAt { get; set; }
    public string? ResponseNote { get; set; }
    public string Priority { get; set; } = string.Empty;
    public int ApprovalLevel { get; set; }
    public int RequiredApprovalLevels { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.UtcNow && Status == "Pending";
}

/// <summary>
/// Onay listesi DTO
/// </summary>
public class ApprovalListDto
{
    public int Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string ApprovalType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? RequestedByUserName { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = string.Empty;
    public bool IsOverdue => DueDate.HasValue && DueDate.Value < DateTime.UtcNow && Status == "Pending";
}

/// <summary>
/// Onay talebi oluşturma DTO
/// </summary>
public class CreateApprovalDto
{
    public string ApprovalType { get; set; } = "General";
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? RelatedEntityId { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? ApproverUserId { get; set; }
    public DateTime? DueDate { get; set; }
    public string Priority { get; set; } = "Normal";
    public int? AutoApproveHours { get; set; }
    public int RequiredApprovalLevels { get; set; } = 1;
}

/// <summary>
/// Onay yanıtı DTO
/// </summary>
public class ApprovalResponseDto
{
    public bool Approved { get; set; }
    public string? Note { get; set; }
}

/// <summary>
/// Onay filtre DTO
/// </summary>
public class ApprovalFilterDto
{
    // Çoklu filtreler (OR mantığı)
    public List<string>? ApprovalTypes { get; set; }
    public List<string>? Statuses { get; set; }
    public List<string>? Priorities { get; set; }
    public List<int>? RequestedByUserIds { get; set; }
    public List<int>? ApproverUserIds { get; set; }

    // Tekil filtreler (çoklu mantıklı değil)
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? IsOverdue { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? SortBy { get; set; }
    public string SortDirection { get; set; } = "desc";
}

/// <summary>
/// Onay özeti DTO
/// </summary>
public class ApprovalSummaryDto
{
    public int TotalApprovals { get; set; }
    public int PendingApprovals { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public int OverdueCount { get; set; }
    public int TodayApprovals { get; set; }
    public List<ApprovalListDto> RecentApprovals { get; set; } = new();
}
