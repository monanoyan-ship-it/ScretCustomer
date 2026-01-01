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
    public int BranchId { get; set; }
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
    public int BranchId { get; set; }
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
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public decimal? ScorePercentage { get; set; }
    public DateTime EvaluationDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Günlük dinleme metrikleri
/// </summary>
public class DailyMetricsDto
{
    /// <summary>Bugün yapılan değerlendirme sayısı</summary>
    public int TodayEvaluations { get; set; }

    /// <summary>Bu hafta yapılan değerlendirme sayısı</summary>
    public int ThisWeekEvaluations { get; set; }

    /// <summary>Bu ay yapılan değerlendirme sayısı</summary>
    public int ThisMonthEvaluations { get; set; }

    /// <summary>Günlük hedef (ayarlardan)</summary>
    public int DailyTarget { get; set; }

    /// <summary>Günlük hedef tamamlanma yüzdesi</summary>
    public decimal DailyTargetPercentage { get; set; }

    /// <summary>Bugün ortalama puan</summary>
    public decimal TodayAverageScore { get; set; }

    /// <summary>Bu hafta ortalama puan</summary>
    public decimal ThisWeekAverageScore { get; set; }

    /// <summary>Son 7 günün günlük trend verileri</summary>
    public List<DailyTrendDto> DailyTrends { get; set; } = new();
}

public class DailyTrendDto
{
    public DateTime Date { get; set; }
    public string DayName { get; set; } = string.Empty;
    public int EvaluationCount { get; set; }
    public decimal AverageScore { get; set; }
}

/// <summary>
/// Kullanıcı performans metrikleri
/// </summary>
public class UserPerformanceDto
{
    /// <summary>Bugün en çok değerlendirme yapan kullanıcılar</summary>
    public List<TopEvaluatorDto> TopEvaluatorsToday { get; set; } = new();

    /// <summary>Bu ay en çok değerlendirme yapan kullanıcılar</summary>
    public List<TopEvaluatorDto> TopEvaluatorsMonth { get; set; } = new();

    /// <summary>Kullanıcı sıralaması (aylık)</summary>
    public List<UserRankingDto> UserRankings { get; set; } = new();
}

public class TopEvaluatorDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int EvaluationCount { get; set; }
    public decimal AverageScore { get; set; }
}

public class UserRankingDto
{
    public int Rank { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int EvaluationCount { get; set; }
    public decimal AverageScore { get; set; }
    public bool IsCurrentUser { get; set; }
}

/// <summary>
/// Hedef takip metrikleri
/// </summary>
public class TargetProgressDto
{
    /// <summary>Aktif dönem bilgisi</summary>
    public string? CurrentPeriodName { get; set; }

    /// <summary>Dönem başlangıç tarihi</summary>
    public DateTime? PeriodStartDate { get; set; }

    /// <summary>Dönem bitiş tarihi</summary>
    public DateTime? PeriodEndDate { get; set; }

    /// <summary>Dönem hedefi</summary>
    public int PeriodTarget { get; set; }

    /// <summary>Dönemde tamamlanan değerlendirme sayısı</summary>
    public int PeriodCompleted { get; set; }

    /// <summary>Dönem tamamlanma yüzdesi</summary>
    public decimal PeriodPercentage { get; set; }

    /// <summary>Kalan değerlendirme sayısı</summary>
    public int Remaining { get; set; }

    /// <summary>Günlük hedef (ayarlardan)</summary>
    public int DailyTarget { get; set; }

    /// <summary>Bugün yapılanlar</summary>
    public int TodayCompleted { get; set; }

    /// <summary>Proje bazlı hedef bilgisi</summary>
    public List<ProjectTargetDto> ProjectTargets { get; set; } = new();
}

public class ProjectTargetDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int Target { get; set; }
    public int Completed { get; set; }
    public decimal Percentage { get; set; }
}
