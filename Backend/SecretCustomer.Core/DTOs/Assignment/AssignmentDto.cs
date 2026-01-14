namespace SecretCustomer.Core.DTOs.Assignment;

/// <summary>
/// Liste görünümü için hafif DTO - Include kullanmadan projection ile çekilir
/// </summary>
public class AssignmentListDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? ProjectCode { get; set; }
    public int ChecklistId { get; set; }
    public string ChecklistName { get; set; } = string.Empty;
    public int? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
    public int? AssignedCustomerPersonnelId { get; set; }
    public string? AssignedCustomerPersonnelName { get; set; }
    public string? ExternalEmail { get; set; }
    public string? ExternalName { get; set; }
    public string UniqueLink { get; set; } = string.Empty;
    public DateTime DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "Pending";

    // Evaluation bilgileri (COUNT ve aggregate ile)
    public int? EvaluationId { get; set; }
    public decimal? EvaluationScore { get; set; }
    public int YellowCardCount { get; set; }
    public int RedCardCount { get; set; }
    public int EvaluationCount { get; set; }

    // Computed properties
    public string AssigneeName => AssignedUserName
        ?? AssignedCustomerPersonnelName
        ?? ExternalName
        ?? ExternalEmail
        ?? "Atanmamış";

    public string AssigneeType => AssignedUserId.HasValue ? "User"
        : AssignedCustomerPersonnelId.HasValue ? "CustomerPersonnel"
        : !string.IsNullOrEmpty(ExternalEmail) ? "External"
        : "Unassigned";

    public int DaysRemaining => (DueDate - DateTime.UtcNow).Days;
}

public class AssignmentDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? ProjectCode { get; set; }
    public int ChecklistId { get; set; }
    public string ChecklistName { get; set; } = string.Empty;
    public int? AssignedUserId { get; set; }
    public string? AssignedUserName { get; set; }
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
    public int EvaluationCount { get; set; } // Toplam dinleme sayısı

    // Assignee display name
    public string AssigneeName => AssignedUserName
        ?? AssignedCustomerPersonnelName
        ?? ExternalName
        ?? ExternalEmail
        ?? "Atanmamış";

    public string AssigneeType => AssignedUserId.HasValue ? "User"
        : AssignedCustomerPersonnelId.HasValue ? "CustomerPersonnel"
        : !string.IsNullOrEmpty(ExternalEmail) ? "External"
        : "Unassigned";
}

/// <summary>
/// Atama detay DTO - Değerlendirme bilgileriyle birlikte
/// </summary>
public class AssignmentDetailDto : AssignmentDto
{
    // Proje Firma/Organizasyon Bilgileri
    public string? CustomerName { get; set; }
    public string? OrganizationName { get; set; }

    public string? EvaluatorName { get; set; }
    public DateTime? EvaluationDate { get; set; }
    public string? EvaluationNotes { get; set; }
    public List<AssignmentHistoryDto> History { get; set; } = new();

    /// <summary>
    /// Bu atamaya ait dönemler
    /// </summary>
    public List<AssignmentPeriodSummaryDto> Periods { get; set; } = new();

    /// <summary>
    /// Projeye ait dosyalar - QualitySpecialist/FieldWorker görebilir
    /// </summary>
    public List<ProjectFileInfoDto> ProjectFiles { get; set; } = new();
}

/// <summary>
/// Proje dosyası özet bilgisi - Assignment detail için
/// </summary>
public class ProjectFileInfoDto
{
    public int Id { get; set; }
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string? Description { get; set; }

    public string FileSizeDisplay
    {
        get
        {
            if (FileSize < 1024) return $"{FileSize} B";
            if (FileSize < 1024 * 1024) return $"{FileSize / 1024.0:F1} KB";
            if (FileSize < 1024 * 1024 * 1024) return $"{FileSize / (1024.0 * 1024):F1} MB";
            return $"{FileSize / (1024.0 * 1024 * 1024):F1} GB";
        }
    }

    public string FileIcon
    {
        get
        {
            if (string.IsNullOrEmpty(ContentType)) return "bi-file-earmark";
            if (ContentType.Contains("pdf")) return "bi-file-earmark-pdf";
            if (ContentType.Contains("word") || ContentType.Contains("document")) return "bi-file-earmark-word";
            if (ContentType.Contains("excel") || ContentType.Contains("spreadsheet")) return "bi-file-earmark-excel";
            if (ContentType.Contains("image")) return "bi-file-earmark-image";
            if (ContentType.Contains("video")) return "bi-file-earmark-play";
            if (ContentType.Contains("audio")) return "bi-file-earmark-music";
            if (ContentType.Contains("zip") || ContentType.Contains("rar") || ContentType.Contains("compressed")) return "bi-file-earmark-zip";
            return "bi-file-earmark";
        }
    }
}

/// <summary>
/// Dönem özet DTO - Assignment detail için
/// </summary>
public class AssignmentPeriodSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Open";
    public int TargetCount { get; set; }
    public int CompletedCount { get; set; }
    public decimal? AverageScore { get; set; }
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

