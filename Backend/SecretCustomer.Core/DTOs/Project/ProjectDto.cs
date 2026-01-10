namespace SecretCustomer.Core.DTOs.Project;

public class ProjectDto
{
    public int Id { get; set; }
    public string? Code { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ChecklistId { get; set; }
    public string ChecklistName { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string AssignmentType { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Istatistikler
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int PendingAssignments { get; set; }
    public decimal CompletionPercentage { get; set; }
    public decimal AverageScore { get; set; }

    // Hedefler
    public int? TargetCount { get; set; }
    public int? DailyQuota { get; set; }
    public int? WeeklyQuota { get; set; }
    public int? MonthlyQuota { get; set; }

    // Butce
    public decimal? EstimatedBudget { get; set; }
    public decimal? ActualBudget { get; set; }
    public decimal? CostPerEvaluation { get; set; }

    // Raporlama
    public int? ReportingFrequencyDays { get; set; }
    public bool AutoGenerateReports { get; set; }
    public DateTime? LastReportDate { get; set; }

    // Yonetici
    public int? ProjectManagerId { get; set; }
    public string? ProjectManagerName { get; set; }

    // Diger
    public decimal? MinimumScoreThreshold { get; set; }
    public string? Priority { get; set; }
    public string? Tags { get; set; }
    public string? Notes { get; set; }

    // Musteri
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }

    // Organizasyon/Sube
    public int? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }

    // Kart sayilari
    public int TotalYellowCards { get; set; }
    public int TotalRedCards { get; set; }

    // Takim ve Subeler
    public int TeamMemberCount { get; set; }
    public int TargetBranchCount { get; set; }
}

/// <summary>
/// Proje detay DTO - Takim ve Sube bilgileriyle birlikte
/// </summary>
public class ProjectDetailDto : ProjectDto
{
    public List<ProjectBranchDto> Branches { get; set; } = new();
    public List<ProjectTeamMemberDto> TeamMembers { get; set; } = new();
    public List<ProjectStatisticDto> DailyStatistics { get; set; } = new();
}

public class ProjectBranchDto
{
    public int Id { get; set; }
    public int BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string? BranchCode { get; set; }
    public int? TargetCount { get; set; }
    public int CompletedCount { get; set; }
    public int Priority { get; set; }
    public bool IsActive { get; set; }
}

public class ProjectTeamMemberDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public bool IsActive { get; set; }
    public int CompletedEvaluations { get; set; }
}

public class ProjectStatisticDto
{
    public DateTime Date { get; set; }
    public int TotalEvaluations { get; set; }
    public int CompletedEvaluations { get; set; }
    public decimal AverageScore { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
}

/// <summary>
/// Proje ozeti - Dashboard icin
/// </summary>
public class ProjectSummaryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ProjectType { get; set; } = string.Empty;
    public decimal CompletionPercentage { get; set; }
    public decimal AverageScore { get; set; }
    public int DaysRemaining { get; set; }
    public bool IsOverdue { get; set; }
}
