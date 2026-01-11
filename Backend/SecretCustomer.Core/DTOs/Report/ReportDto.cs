namespace SecretCustomer.Core.DTOs.Report;

/// <summary>
/// Rapor filtre DTO
/// </summary>
public class ReportFilterDto
{
    // Dropdown filters
    public int? CustomerId { get; set; }
    public int? OrganizationId { get; set; }
    public int? ProjectId { get; set; }
    public int? EvaluatorId { get; set; }
    public int? ChecklistId { get; set; }
    public int? PeriodId { get; set; }

    // Text search filters
    public string? EvaluatedPersonnelName { get; set; }
    public string? SupervisorName { get; set; }
    public string? CallId { get; set; }

    // Date filters
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }

    // Status filter
    public string? Status { get; set; }

    // Sorting
    public string? SortField { get; set; }
    public string? SortDirection { get; set; } = "desc";

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Değerlendirme rapor satırı
/// </summary>
public class EvaluationReportDto
{
    public int EvaluationId { get; set; }
    public int AssignmentId { get; set; }

    // Project
    public string ProjectName { get; set; } = string.Empty;
    public string? ProjectCode { get; set; }

    // Checklist
    public string ChecklistName { get; set; } = string.Empty;

    // Evaluator
    public string? EvaluatorName { get; set; }

    // Personnel
    public string? EvaluatedPersonnelName { get; set; }
    public string? SupervisorName { get; set; }

    // Customer/Organization
    public string? CustomerName { get; set; }
    public string? OrganizationName { get; set; }

    // Period
    public string? PeriodName { get; set; }

    // Dates
    public DateTime? EvaluationDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime DueDate { get; set; }

    // Scores
    public decimal? TotalScore { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? ScorePercentage { get; set; }
    public int YellowCardCount { get; set; }
    public int RedCardCount { get; set; }

    // Status
    public string Status { get; set; } = string.Empty;

    // Call Info
    public string? CallId { get; set; }
    public DateTime? CallDate { get; set; }
    public string? CallTime { get; set; }
    public string? Duration { get; set; }

    // Comment
    public string? Comment { get; set; }
}

/// <summary>
/// Sayfalanmış rapor sonucu
/// </summary>
public class PagedReportResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}

/// <summary>
/// Değerlendirme detay raporu - soru cevaplarıyla birlikte
/// </summary>
public class EvaluationDetailReportDto : EvaluationReportDto
{
    public List<QuestionGroupReportDto> Groups { get; set; } = new();
}

/// <summary>
/// Soru grubu rapor DTO (GroupName'e göre gruplama)
/// </summary>
public class QuestionGroupReportDto
{
    public string GroupName { get; set; } = string.Empty;
    public int Order { get; set; }
    public decimal? GroupScore { get; set; }
    public decimal? GroupMaxScore { get; set; }
    public List<QuestionAnswerReportDto> Questions { get; set; } = new();
}

/// <summary>
/// Soru-cevap rapor DTO
/// </summary>
public class QuestionAnswerReportDto
{
    public string QuestionText { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? AnswerText { get; set; }
    public decimal? AnswerNumeric { get; set; }
    public bool IsNA { get; set; }
    public decimal? GivenPoints { get; set; }
    public decimal? MaxPoints { get; set; }
    public string? PenaltyType { get; set; }
    public string? Notes { get; set; }
    public List<string> SelectedSubCriteria { get; set; } = new();
}

/// <summary>
/// Özet rapor DTO
/// </summary>
public class SummaryReportDto
{
    public int TotalEvaluations { get; set; }
    public int CompletedEvaluations { get; set; }
    public int PendingEvaluations { get; set; }
    public decimal AverageScore { get; set; }
    public decimal MinScore { get; set; }
    public decimal MaxScore { get; set; }
    public int TotalYellowCards { get; set; }
    public int TotalRedCards { get; set; }

    public List<ProjectSummaryReportDto> ProjectSummaries { get; set; } = new();
    public List<EvaluatorSummaryReportDto> EvaluatorSummaries { get; set; } = new();
}

public class ProjectSummaryReportDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public int EvaluationCount { get; set; }
    public decimal AverageScore { get; set; }
}

public class EvaluatorSummaryReportDto
{
    public int EvaluatorId { get; set; }
    public string EvaluatorName { get; set; } = string.Empty;
    public int EvaluationCount { get; set; }
    public decimal AverageScore { get; set; }
}

/// <summary>
/// Excel export için raw data DTO
/// </summary>
public class ExcelExportDto
{
    public string FileName { get; set; } = string.Empty;
    public byte[] FileContent { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
}

// ===== CEZALI KL RAPORU DTO'LARI =====

/// <summary>
/// Cezalı KL raporu filtre DTO
/// </summary>
public class PenaltyFilterDto
{
    public int? ProjectId { get; set; }
    public string? PenaltyType { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Cezalı KL raporu ana sonuç DTO
/// </summary>
public class PenaltyReportResultDto
{
    public PenaltySummaryDto Summary { get; set; } = new();
    public List<PenaltyDetailDto> Penalties { get; set; } = new();
    public List<PenaltyQuestionDto> TopPenaltyQuestions { get; set; } = new();
    public List<PenaltyMonthlyTrendDto> MonthlyTrend { get; set; } = new();
}

/// <summary>
/// Cezalı KL raporu özet
/// </summary>
public class PenaltySummaryDto
{
    public int TotalPenalties { get; set; }
    public int TotalYellowCards { get; set; }
    public int TotalRedCards { get; set; }
    public int AffectedEvaluations { get; set; }
}

/// <summary>
/// Cezalı değerlendirme detay satırı
/// </summary>
public class PenaltyDetailDto
{
    public int EvaluationId { get; set; }
    public int AnswerId { get; set; }
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string PenaltyType { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? ChecklistName { get; set; }
    public string? EvaluatorName { get; set; }
    public string? EvaluatedPersonnelName { get; set; }
    public DateTime? EvaluationDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// En çok ceza alan soru
/// </summary>
public class PenaltyQuestionDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string ChecklistName { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int YellowCardCount { get; set; }
    public int RedCardCount { get; set; }
    public int TotalPenalties { get; set; }
}

/// <summary>
/// Aylık ceza trendi
/// </summary>
public class PenaltyMonthlyTrendDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int YellowCardCount { get; set; }
    public int RedCardCount { get; set; }
    public int TotalPenalties { get; set; }
}

// ===== TEMSİLCİ KARNESİ DTO'LARI (Video 4) =====

/// <summary>
/// Temsilci Karnesi filtre DTO
/// </summary>
public class PersonnelReportCardFilterDto
{
    public int PersonnelId { get; set; }
    public int? ProjectId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}

/// <summary>
/// Temsilci Karnesi ana sonuç DTO
/// </summary>
public class PersonnelReportCardDto
{
    // Personel bilgileri
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Department { get; set; }

    // Özet istatistikler
    public int TotalEvaluations { get; set; }
    public decimal AverageScore { get; set; }
    public decimal BestScore { get; set; }
    public decimal WorstScore { get; set; }
    public int TotalYellowCards { get; set; }
    public int TotalRedCards { get; set; }

    // Performans trendi
    public List<PersonnelMonthlyTrendDto> MonthlyTrend { get; set; } = new();

    // Grup bazlı performans
    public List<PersonnelGroupPerformanceDto> GroupPerformances { get; set; } = new();

    // Son değerlendirmeler listesi
    public List<PersonnelEvaluationSummaryDto> RecentEvaluations { get; set; } = new();

    // Güçlü ve zayıf yönler
    public List<PersonnelStrengthWeaknessDto> Strengths { get; set; } = new();
    public List<PersonnelStrengthWeaknessDto> Weaknesses { get; set; } = new();
}

/// <summary>
/// Aylık performans trendi
/// </summary>
public class PersonnelMonthlyTrendDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public string MonthName { get; set; } = string.Empty;
    public int EvaluationCount { get; set; }
    public decimal AverageScore { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
}

/// <summary>
/// Grup bazlı performans
/// </summary>
public class PersonnelGroupPerformanceDto
{
    public string GroupName { get; set; } = string.Empty;
    public int EvaluationCount { get; set; }
    public decimal AverageScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public decimal PercentageScore { get; set; }
}

/// <summary>
/// Değerlendirme özeti
/// </summary>
public class PersonnelEvaluationSummaryDto
{
    public int EvaluationId { get; set; }
    public DateTime? EvaluationDate { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ChecklistName { get; set; } = string.Empty;
    public string? EvaluatorName { get; set; }
    public decimal ScorePercentage { get; set; }
    public int YellowCards { get; set; }
    public int RedCards { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Güçlü/Zayıf yön analizi
/// </summary>
public class PersonnelStrengthWeaknessDto
{
    public string QuestionText { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
    public decimal MaxScore { get; set; }
    public decimal PercentageScore { get; set; }
    public int EvaluationCount { get; set; }
}

/// <summary>
/// Personel listesi (seçim için)
/// </summary>
public class PersonnelListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
}

// ===== ÖNERİLER RAPORU DTO'LARI (Video 5-6) =====

/// <summary>
/// Öneriler Raporu filtre DTO
/// </summary>
public class SuggestionsFilterDto
{
    public int? ProjectId { get; set; }
    public int? ChecklistId { get; set; }
    public int? EvaluatorId { get; set; }
    public int? PersonnelId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? SearchText { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Öneriler Raporu ana sonuç DTO
/// </summary>
public class SuggestionsReportResultDto
{
    public SuggestionsSummaryDto Summary { get; set; } = new();
    public List<SuggestionDetailDto> Suggestions { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Öneriler raporu özet
/// </summary>
public class SuggestionsSummaryDto
{
    public int TotalSuggestions { get; set; }
    public int TotalEvaluationsWithSuggestions { get; set; }
    public int UniqueEvaluators { get; set; }
    public int UniquePersonnel { get; set; }
}

/// <summary>
/// Öneri detay satırı
/// </summary>
public class SuggestionDetailDto
{
    public int EvaluationId { get; set; }
    public int AnswerId { get; set; }
    public int QuestionId { get; set; }

    // Soru bilgileri
    public string QuestionText { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string ChecklistName { get; set; } = string.Empty;

    // Öneri/Not içeriği
    public string? Notes { get; set; }
    public string? RecommendationNotes { get; set; }

    // Puan bilgisi
    public decimal? GivenPoints { get; set; }
    public decimal? MaxPoints { get; set; }
    public decimal? PercentageScore { get; set; }

    // Proje
    public string ProjectName { get; set; } = string.Empty;

    // Değerlendirici ve personel
    public string? EvaluatorName { get; set; }
    public string? EvaluatedPersonnelName { get; set; }

    // Tarih
    public DateTime? EvaluationDate { get; set; }

    // Ek bilgiler
    public string? CallId { get; set; }
    public bool IsPenaltyApplied { get; set; }
    public string? PenaltyType { get; set; }
}

/// <summary>
/// Soru bazlı öneri özeti (en çok öneri yazılan sorular)
/// </summary>
public class QuestionSuggestionSummaryDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string ChecklistName { get; set; } = string.Empty;
    public int SuggestionCount { get; set; }
    public decimal AverageScore { get; set; }
}
