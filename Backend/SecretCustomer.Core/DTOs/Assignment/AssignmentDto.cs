namespace SecretCustomer.Core.DTOs.Assignment;

public class AssignmentDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? ProjectCode { get; set; }
    public int ChecklistId { get; set; }
    public string ChecklistName { get; set; } = string.Empty;
    public int? BranchId { get; set; }
    public string? BranchName { get; set; }
    public string? BranchCode { get; set; }
    public int? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public int? AssignedFieldWorkerId { get; set; }
    public string? AssignedFieldWorkerName { get; set; }
    public int? AssignedCustomerPersonnelId { get; set; }
    public string? AssignedCustomerPersonnelName { get; set; }
    public string? ExternalEmail { get; set; }
    public string? ExternalName { get; set; }
    public string UniqueLink { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }

    // Status
    public string Status { get; set; } = "Pending";
    public bool IsExpired => !IsCompleted && DueDate < DateTime.UtcNow;
    public int DaysRemaining => (DueDate - DateTime.UtcNow).Days;

    // Evaluation Info
    public int? EvaluationId { get; set; }
    public string? EvaluationStatus { get; set; }
    public decimal? EvaluationScore { get; set; }
    public int YellowCardCount { get; set; }
    public int RedCardCount { get; set; }

    // Assignee display name
    public string AssigneeName => AssignedUserName
        ?? AssignedFieldWorkerName
        ?? AssignedCustomerPersonnelName
        ?? ExternalName
        ?? ExternalEmail
        ?? "Atanmamış";

    public string AssigneeType => AssignedUserId.HasValue ? "User"
        : AssignedFieldWorkerId.HasValue ? "FieldWorker"
        : AssignedCustomerPersonnelId.HasValue ? "CustomerPersonnel"
        : !string.IsNullOrEmpty(ExternalEmail) ? "External"
        : "Unassigned";
}

/// <summary>
/// Atama detay DTO - Değerlendirme bilgileriyle birlikte
/// </summary>
public class AssignmentDetailDto : AssignmentDto
{
    public string? EvaluatorName { get; set; }
    public DateTime? EvaluationDate { get; set; }
    public string? EvaluationNotes { get; set; }
    public List<AssignmentHistoryDto> History { get; set; } = new();
}

/// <summary>
/// Atama geçmişi
/// </summary>
public class AssignmentHistoryDto
{
    public DateTime Timestamp { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? PerformedBy { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Atama özeti - Dashboard için
/// </summary>
public class AssignmentSummaryDto
{
    public int TotalAssignments { get; set; }
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int CompletedCount { get; set; }
    public int ExpiredCount { get; set; }
    public int CancelledCount { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AverageScore { get; set; }
    public int TotalYellowCards { get; set; }
    public int TotalRedCards { get; set; }
}

/// <summary>
/// Proje bazlı atama özeti
/// </summary>
public class ProjectAssignmentSummaryDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int PendingAssignments { get; set; }
    public decimal CompletionPercentage { get; set; }
}

/// <summary>
/// Şube bazlı atama özeti
/// </summary>
public class BranchAssignmentSummaryDto
{
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string? BranchCode { get; set; }
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public decimal AverageScore { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
}
