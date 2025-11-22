namespace SecretCustomer.Core.DTOs.Report;

public class EvaluationReportDto
{
    public Guid EvaluationId { get; set; }
    public Guid AssignmentId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string BranchCity { get; set; } = string.Empty;
    public string EvaluatorName { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
    public int TotalScore { get; set; }
    public int MaxScore { get; set; }
    public double PercentageScore { get; set; }
    public List<SectionScoreDto> SectionScores { get; set; } = new();
}

public class SectionScoreDto
{
    public string SectionName { get; set; } = string.Empty;
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public double Percentage { get; set; }
}

public class BranchPerformanceReportDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public int TotalEvaluations { get; set; }
    public double AverageScore { get; set; }
    public double MinScore { get; set; }
    public double MaxScore { get; set; }
    public List<EvaluationSummaryDto> RecentEvaluations { get; set; } = new();
}

public class EvaluationSummaryDto
{
    public Guid EvaluationId { get; set; }
    public DateTime CompletedAt { get; set; }
    public int Score { get; set; }
    public int MaxScore { get; set; }
    public string EvaluatorName { get; set; } = string.Empty;
}

public class ProjectReportDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalAssignments { get; set; }
    public int CompletedAssignments { get; set; }
    public int PendingAssignments { get; set; }
    public double CompletionRate { get; set; }
    public double AverageScore { get; set; }
    public List<BranchScoreSummaryDto> BranchScores { get; set; } = new();
}

public class BranchScoreSummaryDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public int EvaluationCount { get; set; }
    public double AverageScore { get; set; }
}

public class ReportFilterDto
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? BranchId { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? EvaluatorId { get; set; }
}
