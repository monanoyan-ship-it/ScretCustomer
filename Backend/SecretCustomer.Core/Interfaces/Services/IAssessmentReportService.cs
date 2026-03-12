namespace SecretCustomer.Core.Interfaces.Services;

public interface IAssessmentReportService
{
    /// <summary>
    /// Kişi bazlı rapor: FeedbackRole gruplu ortalama puanlar + yetkinlik bazlı kırılım
    /// </summary>
    Task<AssessmentPersonReport> GetPersonReportAsync(int projectId, int evaluatedCustomerPersonnelId, int? assignmentPeriodId = null);

    /// <summary>
    /// Dönem özeti: Tüm katılımcılar için Self vs Diğerleri + Gap
    /// </summary>
    Task<AssessmentPeriodSummary> GetPeriodSummaryAsync(int projectId, int assignmentPeriodId);
}

// ===== Report DTOs =====

/// <summary>
/// Kişi bazlı değerlendirme raporu
/// </summary>
public class AssessmentPersonReport
{
    public int EvaluatedCustomerPersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;

    /// <summary>
    /// Rol bazlı genel ortalamalar (Self, Manager, Peer, Subordinate)
    /// </summary>
    public List<AssessmentRoleScore> RoleScores { get; set; } = new();

    /// <summary>
    /// Yetkinlik bazlı (GroupName) rol kırılımlı ortalamalar
    /// </summary>
    public List<AssessmentCompetencyScore> CompetencyScores { get; set; } = new();

    /// <summary>
    /// Kör nokta tespiti: Self ile diğerleri arasında büyük fark olan yetkinlikler
    /// </summary>
    public List<AssessmentBlindSpot> BlindSpots { get; set; } = new();

    /// <summary>
    /// Toplam değerlendirme sayısı (role göre)
    /// </summary>
    public int TotalEvaluationCount { get; set; }

    /// <summary>
    /// Anonimlik nedeniyle gizlenen roller
    /// </summary>
    public List<int> HiddenRoleIds { get; set; } = new();
}

/// <summary>
/// Rol bazlı ortalama puan
/// </summary>
public class AssessmentRoleScore
{
    public int FeedbackRoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public decimal AverageScorePercentage { get; set; }
    public int EvaluationCount { get; set; }
    public bool IsHidden { get; set; }
}

/// <summary>
/// Yetkinlik bazlı (GroupName) rol kırılımlı ortalamalar
/// </summary>
public class AssessmentCompetencyScore
{
    public string CompetencyName { get; set; } = string.Empty;

    /// <summary>
    /// Her rol için bu yetkinlikteki ortalama
    /// </summary>
    public List<AssessmentRoleScore> RoleScores { get; set; } = new();

    /// <summary>
    /// Self vs Diğerleri farkı (Gap)
    /// </summary>
    public decimal? Gap { get; set; }
}

/// <summary>
/// Kör nokta tespiti
/// </summary>
public class AssessmentBlindSpot
{
    public string CompetencyName { get; set; } = string.Empty;
    public decimal SelfScore { get; set; }
    public decimal OthersScore { get; set; }
    public decimal Gap { get; set; }

    /// <summary>
    /// Pozitif gap = kişi kendini yüksek görüyor, negatif = düşük görüyor
    /// </summary>
    public string Direction => Gap > 0 ? "Overestimate" : "Underestimate";
}

/// <summary>
/// Dönem bazlı tüm personel özeti
/// </summary>
public class AssessmentPeriodSummary
{
    public int ProjectId { get; set; }
    public int AssignmentPeriodId { get; set; }
    public List<AssessmentPersonSummaryRow> PersonnelSummaries { get; set; } = new();
}

/// <summary>
/// Dönem özetinde her personel satırı
/// </summary>
public class AssessmentPersonSummaryRow
{
    public int CustomerPersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public decimal? SelfScore { get; set; }
    public decimal? ManagerScore { get; set; }
    public decimal? PeerScore { get; set; }
    public decimal? SubordinateScore { get; set; }
    public decimal? OthersAverage { get; set; }
    public decimal? Gap { get; set; }
    public int TotalEvaluationCount { get; set; }
}
