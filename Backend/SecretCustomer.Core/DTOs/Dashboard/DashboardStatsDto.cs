namespace SecretCustomer.Core.DTOs.Dashboard;

public class DashboardStatsDto
{
    public int TotalEvaluations { get; set; }
    public decimal AverageScore { get; set; }
    public decimal PercentageChange { get; set; } // Önceki aya göre
    public List<TopBranchDto> TopBranches { get; set; } = new();
    public List<TopBranchDto> BottomBranches { get; set; } = new();
    public List<MonthlyTrendDto> MonthlyTrends { get; set; } = new();
    public List<BranchComparisonDto> BranchComparisons { get; set; } = new();
}

public class TopBranchDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
    public int EvaluationCount { get; set; }
}

public class MonthlyTrendDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
    public int EvaluationCount { get; set; }
}

public class BranchComparisonDto
{
    public Guid BranchId { get; set; }
    public string BranchName { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
    public int EvaluationCount { get; set; }
}
