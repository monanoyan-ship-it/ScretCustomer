using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.DTOs.CustomerVisit;

/// <summary>
/// Müşteri Ziyaret DTO
/// </summary>
public class CustomerVisitDto
{
    public Guid Id { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public Guid VisitorUserId { get; set; }
    public string? VisitorUserName { get; set; }
    public VisitType VisitType { get; set; }
    public string VisitTypeName => VisitType.ToString();
    public VisitStatus Status { get; set; }
    public string StatusName => Status.ToString();
    public DateTime PlannedDate { get; set; }
    public string? PlannedTime { get; set; }
    public DateTime? ActualDate { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Purpose { get; set; }
    public string? Notes { get; set; }
    public string? Observations { get; set; }
    public string? ContactPersonName { get; set; }
    public string? ContactPersonTitle { get; set; }
    public string? ContactPersonPhone { get; set; }
    public string? Address { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool RequiresFollowUp { get; set; }
    public string? FollowUpNotes { get; set; }
    public DateTime? FollowUpDate { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? EvaluationId { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CustomerVisitAttachmentDto> Attachments { get; set; } = new();
}

/// <summary>
/// Ziyaret oluşturma/güncelleme DTO
/// </summary>
public class CreateCustomerVisitDto
{
    public Guid? CustomerId { get; set; }
    public Guid? BranchId { get; set; }
    public VisitType VisitType { get; set; } = VisitType.Routine;
    public DateTime PlannedDate { get; set; }
    public string? PlannedTime { get; set; }
    public string? Purpose { get; set; }
    public string? ContactPersonName { get; set; }
    public string? ContactPersonTitle { get; set; }
    public string? ContactPersonPhone { get; set; }
    public string? Address { get; set; }
    public Guid? ProjectId { get; set; }
}

/// <summary>
/// Ziyaret tamamlama DTO
/// </summary>
public class CompleteCustomerVisitDto
{
    public DateTime? ActualDate { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationMinutes { get; set; }
    public string? Notes { get; set; }
    public string? Observations { get; set; }
    public string? ContactPersonName { get; set; }
    public string? ContactPersonTitle { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public bool RequiresFollowUp { get; set; }
    public string? FollowUpNotes { get; set; }
    public DateTime? FollowUpDate { get; set; }
}

/// <summary>
/// Ziyaret eki DTO
/// </summary>
public class CustomerVisitAttachmentDto
{
    public Guid Id { get; set; }
    public Guid CustomerVisitId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }
    public AttachmentType AttachmentType { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DownloadUrl => $"/api/customer-visits/attachments/{Id}/download";
}

/// <summary>
/// Ziyaret listesi için özet DTO
/// </summary>
public class CustomerVisitSummaryDto
{
    public Guid Id { get; set; }
    public string? CustomerName { get; set; }
    public string? BranchName { get; set; }
    public string? VisitorUserName { get; set; }
    public VisitType VisitType { get; set; }
    public VisitStatus Status { get; set; }
    public DateTime PlannedDate { get; set; }
    public DateTime? ActualDate { get; set; }
    public bool RequiresFollowUp { get; set; }
    public int AttachmentCount { get; set; }
}

/// <summary>
/// Ziyaret filtre DTO
/// </summary>
public class CustomerVisitFilterDto
{
    public Guid? CustomerId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? VisitorUserId { get; set; }
    public VisitType? VisitType { get; set; }
    public VisitStatus? Status { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? RequiresFollowUp { get; set; }
}
