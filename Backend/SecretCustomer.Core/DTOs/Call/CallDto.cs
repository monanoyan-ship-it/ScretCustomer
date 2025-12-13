namespace SecretCustomer.Core.DTOs.Call;

/// <summary>
/// Çağrı DTO
/// </summary>
public class CallDto
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string CallType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public string? CallerPhone { get; set; }
    public string? CallerName { get; set; }
    public string? CalledPhone { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int? DurationSeconds { get; set; }
    public int? WaitTimeSeconds { get; set; }
    public string? Notes { get; set; }
    public string? Outcome { get; set; }
    public string? RecordingFileName { get; set; }
    public bool HasRecording { get; set; }
    public string? ExternalCallId { get; set; }
    public Guid? ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public Guid? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public Guid? BranchId { get; set; }
    public string? BranchName { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public Guid? EvaluationId { get; set; }
    public string? Tags { get; set; }
    public bool RequiresCallback { get; set; }
    public DateTime? CallbackDate { get; set; }
    public string? CallbackNote { get; set; }
    public bool CallbackCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CallAttachmentDto> Attachments { get; set; } = new();
    public string DurationFormatted => DurationSeconds.HasValue
        ? $"{DurationSeconds.Value / 60}:{(DurationSeconds.Value % 60):D2}"
        : "-";
}

/// <summary>
/// Çağrı listesi için özet DTO
/// </summary>
public class CallListDto
{
    public Guid Id { get; set; }
    public string ReferenceNumber { get; set; } = string.Empty;
    public string CallType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? CallerPhone { get; set; }
    public string? CallerName { get; set; }
    public DateTime? StartTime { get; set; }
    public int? DurationSeconds { get; set; }
    public string? ProjectName { get; set; }
    public string? BranchName { get; set; }
    public string? AssignedUserName { get; set; }
    public bool RequiresCallback { get; set; }
    public bool CallbackCompleted { get; set; }
    public bool HasRecording { get; set; }
    public string DurationFormatted => DurationSeconds.HasValue
        ? $"{DurationSeconds.Value / 60}:{(DurationSeconds.Value % 60):D2}"
        : "-";
}

/// <summary>
/// Çağrı oluşturma DTO
/// </summary>
public class CreateCallDto
{
    public string CallType { get; set; } = "Inbound";
    public string Priority { get; set; } = "Normal";
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public string? CallerPhone { get; set; }
    public string? CallerName { get; set; }
    public string? CalledPhone { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? Tags { get; set; }
}

/// <summary>
/// Çağrı güncelleme DTO
/// </summary>
public class UpdateCallDto
{
    public string CallType { get; set; } = "Inbound";
    public string Status { get; set; } = "Pending";
    public string Priority { get; set; } = "Normal";
    public string? Subject { get; set; }
    public string? Description { get; set; }
    public string? CallerPhone { get; set; }
    public string? CallerName { get; set; }
    public string? CalledPhone { get; set; }
    public DateTime? ScheduledDate { get; set; }
    public string? Notes { get; set; }
    public string? Outcome { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? Tags { get; set; }
    public bool RequiresCallback { get; set; }
    public DateTime? CallbackDate { get; set; }
    public string? CallbackNote { get; set; }
}

/// <summary>
/// Çağrı başlatma DTO
/// </summary>
public class StartCallDto
{
    public DateTime? StartTime { get; set; }
}

/// <summary>
/// Çağrı tamamlama DTO
/// </summary>
public class CompleteCallDto
{
    public DateTime? EndTime { get; set; }
    public int? DurationSeconds { get; set; }
    public string? Notes { get; set; }
    public string? Outcome { get; set; }
    public bool RequiresCallback { get; set; }
    public DateTime? CallbackDate { get; set; }
    public string? CallbackNote { get; set; }
}

/// <summary>
/// Çağrı kaydı yükleme DTO
/// </summary>
public class UploadRecordingDto
{
    public string? Description { get; set; }
}

/// <summary>
/// Çağrı eki DTO
/// </summary>
public class CallAttachmentDto
{
    public Guid Id { get; set; }
    public Guid CallId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? ContentType { get; set; }
    public string? Description { get; set; }
    public string? UploadedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DownloadUrl => $"/api/calls/{CallId}/attachments/{Id}/download";
}

/// <summary>
/// Çağrı filtre DTO
/// </summary>
public class CallFilterDto
{
    public Guid? ProjectId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? AssignedUserId { get; set; }
    public string? CallType { get; set; }
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool? RequiresCallback { get; set; }
    public bool? HasRecording { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Dashboard için çağrı özeti
/// </summary>
public class CallSummaryDto
{
    public int TotalCalls { get; set; }
    public int TodayCalls { get; set; }
    public int PendingCalls { get; set; }
    public int CompletedCalls { get; set; }
    public int MissedCalls { get; set; }
    public int CallbackRequired { get; set; }
    public int AverageDurationSeconds { get; set; }
    public int AverageWaitTimeSeconds { get; set; }
    public List<CallListDto> RecentCalls { get; set; } = new();
}
