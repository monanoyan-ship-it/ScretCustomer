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

/// <summary>
/// Kişisel performans kartı (Scorecard)
/// </summary>
public class ScorecardDto
{
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;

    // Bu ay
    public int CurrentMonthEvaluations { get; set; }
    public decimal CurrentMonthAverage { get; set; }

    // Geçen ay
    public int LastMonthEvaluations { get; set; }
    public decimal LastMonthAverage { get; set; }

    // Toplam
    public int TotalEvaluations { get; set; }
    public decimal TotalAverage { get; set; }

    // Değişim
    public decimal MonthlyChange { get; set; } // Bu ay - Geçen ay

    // Takım/Şirket karşılaştırması
    public decimal TeamAverage { get; set; }
    public decimal CompanyAverage { get; set; }

    // Sıralama
    public int UserRank { get; set; }
    public int TotalUsers { get; set; }

    // Son 5 değerlendirme
    public List<RecentEvaluationDto> RecentEvaluations { get; set; } = new();
}

public class RecentEvaluationDto
{
    public Guid Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public decimal? ScorePercentage { get; set; }
    public DateTime EvaluationDate { get; set; }
    public string Status { get; set; } = string.Empty;
}
