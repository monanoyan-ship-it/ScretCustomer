namespace SecretCustomer.Core.DTOs.Report;

/// <summary>
/// Rapor filtre DTO - Sadece çoklu filtreler (OR mantığı)
/// Tek değer için [5] şeklinde array gönder
/// </summary>
public class ReportFilterDto
{
    public List<int>? CustomerIds { get; set; }
    public List<int>? ProjectCustomerIds { get; set; }
    public List<int>? OrganizationIds { get; set; }
    public List<int>? ProjectIds { get; set; }
    public List<string>? ProjectTypes { get; set; }
    public List<int>? EvaluatorIds { get; set; }
    public List<int>? ChecklistIds { get; set; }
    public List<int>? PeriodIds { get; set; }
    public List<string>? PersonnelNames { get; set; }
    public List<string>? SupervisorNames { get; set; }
    public List<string>? CallIds { get; set; }
    public List<string>? Statuses { get; set; }
    public List<string>? EvaluationSources { get; set; }
    public List<DateRangeFilter>? DateRanges { get; set; }

    // Sorting
    public string? SortField { get; set; }
    public string? SortDirection { get; set; } = "desc";

    // Pagination
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;

    public bool SkipCount { get; set; } = false;
    public bool ForExport { get; set; } = false;
}

/// <summary>
/// Tarih aralığı filtresi
/// </summary>
public class DateRangeFilter
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    /// <summary>
    /// Filtre tipi: "start" = Başlangıç tarihi, "due" = Bitiş tarihi
    /// </summary>
    public string? FilterType { get; set; }
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

    /// <summary>
    /// Düz liste halinde tüm soru-cevaplar (Online Anket detayı için)
    /// </summary>
    public List<FlatQuestionAnswerDto> QuestionAnswers { get; set; } = new();

    /// <summary>
    /// Dinleme detay modalı için düz cevap listesi
    /// </summary>
    public List<EvaluationAnswerDto> Answers { get; set; } = new();

    /// <summary>
    /// Değerlendirme yorumu (EvaluationComment alanı)
    /// </summary>
    public string? EvaluationComment { get; set; }

    /// <summary>
    /// Genel notlar
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Dinleme detay modalı için cevap DTO
/// </summary>
public class EvaluationAnswerDto
{
    public string? GroupName { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public decimal? GivenPoints { get; set; }
    public decimal MaxPoints { get; set; }
    public string? AppliedPenaltyType { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Düz soru-cevap DTO (grup bilgisiyle birlikte, online anket detayı için)
/// </summary>
public class FlatQuestionAnswerDto
{
    public string QuestionText { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public int Order { get; set; }
    public decimal? Score { get; set; }
    public decimal? MaxPoints { get; set; }
    public List<string> SelectedSubCriteria { get; set; } = new();
    public string? Comment { get; set; }
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
/// Cezalı KL raporu filtre DTO - Çoklu filtreler (OR mantığı)
/// Tekil property'ler geriye uyumluluk için - setter değeri array'e ekler
/// </summary>
public class PenaltyFilterDto
{
    // Çoklu filtreler (OR mantığı)
    public List<int>? ProjectIds { get; set; }
    public List<int>? CustomerIds { get; set; }
    public List<int>? OrganizationIds { get; set; }
    public List<int>? ChecklistIds { get; set; }
    public List<int>? EvaluatorIds { get; set; }
    public List<string>? PenaltyTypes { get; set; }
    public List<DateRangeFilter>? DateRanges { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Cezalı KL raporu ana sonuç DTO
/// </summary>
public class PenaltyReportResultDto
{
    public PenaltySummaryDto Summary { get; set; } = new();
    public List<PenaltyDetailDto> Penalties { get; set; } = new();
    public List<PenaltyQuestionDto> TopPenaltyQuestions { get; set; } = new();
    public List<PenaltyOrganizationDto> TopPenaltyOrganizations { get; set; } = new();
    public List<PenaltyPersonnelDto> TopPenaltyPersonnel { get; set; } = new();
    public List<PenaltyMonthlyTrendDto> MonthlyTrend { get; set; } = new();

    // Pagination info
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
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
    public string? CallId { get; set; }
    public string? CallTime { get; set; }
    public string? Duration { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public string PenaltyType { get; set; } = string.Empty;
    public string ProjectName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? OrganizationName { get; set; }
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
/// En çok ceza alan organizasyon/şube
/// </summary>
public class PenaltyOrganizationDto
{
    public int OrganizationId { get; set; }
    public string OrganizationName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public int YellowCardCount { get; set; }
    public int RedCardCount { get; set; }
    public int TotalPenalties { get; set; }
}

/// <summary>
/// En çok ceza alan personel
/// </summary>
public class PenaltyPersonnelDto
{
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public int YellowCardCount { get; set; }
    public int RedCardCount { get; set; }
    public int TotalPenalties { get; set; }
    public int EvaluationCount { get; set; }
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
    public List<int>? ProjectIds { get; set; }
    public List<DateRangeFilter>? DateRanges { get; set; }
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
    public string? CallId { get; set; }
    public string? CallTime { get; set; }
    public string? Duration { get; set; }
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
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
}

/// <summary>
/// Organizasyon listesi (rapor filtresi için)
/// </summary>
public class OrganizationListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int EvaluationCount { get; set; }
}

/// <summary>
/// Proje listesi (basit dropdown/filtre için)
/// </summary>
public class ProjectListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ===== ÖNERİLER RAPORU DTO'LARI (Video 5-6) =====

/// <summary>
/// Öneriler Raporu filtre DTO - Çoklu filtreler (OR mantığı)
/// Tekil property'ler geriye uyumluluk için - setter değeri array'e ekler
/// </summary>
public class SuggestionsFilterDto
{
    // Çoklu filtreler (OR mantığı)
    public List<int>? ProjectIds { get; set; }
    public List<int>? CustomerIds { get; set; }
    public List<int>? OrganizationIds { get; set; }
    public List<int>? ChecklistIds { get; set; }
    public List<int>? EvaluatorIds { get; set; }
    public List<int>? PersonnelIds { get; set; }
    public List<DateRangeFilter>? DateRanges { get; set; }
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
    public int EvaluationCount { get; set; }
    public decimal AverageScore { get; set; }
}

// ===== SORU GRUBU ORTALAMA RAPORU DTO'LARI =====

/// <summary>
/// Soru Grubu Ortalama Raporu satırı
/// Proje + Periyot + Kontrol Grubu bazında ortalama puan
/// </summary>
public class QuestionGroupAverageReportDto
{
    /// <summary>
    /// Proje adı (periyot dahil: "Proje Adı Aralık2025")
    /// </summary>
    public string ProjectName { get; set; } = string.Empty;

    /// <summary>
    /// Kontrol grubu adı (sıra numarası dahil: "1 Çağrı Standartlarına Uyum")
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Periyot (Yıl)
    /// </summary>
    public int Year { get; set; }

    /// <summary>
    /// Ortalama puan (yüzde)
    /// </summary>
    public decimal AverageScore { get; set; }

    /// <summary>
    /// Değerlendirme sayısı
    /// </summary>
    public int EvaluationCount { get; set; }
}

// ===== ANKET SONUÇLARI RAPORU DTO'LARI =====

/// <summary>
/// Anket Sonuçları ana DTO
/// </summary>
public class SurveyResultsDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? OrganizationName { get; set; }

    // Özet istatistikler
    public int TotalResponses { get; set; }
    public int TotalInvited { get; set; }
    public decimal CompletionRate { get; set; }
    public decimal AverageScore { get; set; }
    public int TotalQuestions { get; set; }

    // Soru bazlı sonuçlar
    public List<SurveyQuestionResultDto> QuestionResults { get; set; } = new();

    // Katılımcı listesi
    public List<SurveyRespondentDto> Respondents { get; set; } = new();
}

/// <summary>
/// Soru bazlı anket sonucu
/// </summary>
public class SurveyQuestionResultDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string GroupName { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool ShowScoreInput { get; set; }

    // Yanıt istatistikleri
    public int ResponseCount { get; set; }
    public decimal? AverageScore { get; set; }

    // Puan dağılımı (showScoreInput true ise)
    public List<ScoreDistributionDto>? ScoreDistribution { get; set; }

    // Alt kriter seçim sonuçları
    public List<SubCriteriaResultDto>? SubCriteriaResults { get; set; }

    // Yorumlar
    public List<SurveyCommentDto>? Comments { get; set; }
}

/// <summary>
/// Puan dağılımı
/// </summary>
public class ScoreDistributionDto
{
    public int Score { get; set; }
    public int Count { get; set; }
    public decimal Percentage { get; set; }
}

/// <summary>
/// Alt kriter seçim sonucu
/// </summary>
public class SubCriteriaResultDto
{
    public int SubCriteriaId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int SelectionCount { get; set; }
    public decimal SelectionPercentage { get; set; }
}

/// <summary>
/// Anket yorumu
/// </summary>
public class SurveyCommentDto
{
    public int EvaluationId { get; set; }
    public string? RespondentName { get; set; }
    public string Comment { get; set; } = string.Empty;
    public DateTime? Date { get; set; }
}

/// <summary>
/// Anket katılımcısı
/// </summary>
public class SurveyRespondentDto
{
    public int PersonnelId { get; set; }
    public int? EvaluationId { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
    public string? OrganizationName { get; set; }
    public decimal? Score { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Online Anket Proje Listesi için DTO (dashboard panel)
/// </summary>
public class SurveyProjectListItemDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? ProjectCode { get; set; }
    public int TotalInvitations { get; set; }
    public int TotalResponses { get; set; }
    public decimal ResponseRate { get; set; }
    public decimal? AverageScore { get; set; }
    public DateTime? LastResponseAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Son Anket Yanıtları için DTO (dashboard sol panel)
/// </summary>
public class RecentSurveyResponseDto
{
    public int EvaluationId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? RespondentName { get; set; }
    public string? RespondentEmail { get; set; }
    public decimal? Score { get; set; }
    public DateTime? CompletedAt { get; set; }
}

/// <summary>
/// Anket Proje Detay DTO (modal için)
/// </summary>
public class SurveyProjectDetailDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }
    public string? OrganizationName { get; set; }

    // Genel istatistikler
    public int TotalInvitations { get; set; }
    public int TotalResponses { get; set; }
    public decimal ResponseRate { get; set; }
    public decimal? AverageScore { get; set; }
    public int TotalQuestions { get; set; }

    // Grup bazlı puan hesaplaması
    public List<SurveyGroupScoreDto> GroupScores { get; set; } = new();

    // Son yanıtlar
    public List<SurveyRespondentDto> RecentRespondents { get; set; } = new();
}

/// <summary>
/// Anket Grup Bazlı Puan DTO
/// </summary>
public class SurveyGroupScoreDto
{
    public string GroupName { get; set; } = string.Empty;
    public int QuestionCount { get; set; }
    public int TotalResponses { get; set; }
    public decimal? AverageScore { get; set; }
}

// ===== PERFORMANS TAKİBİ RAPORU DTO'LARI =====

/// <summary>
/// Performans Takibi ana sonuç DTO
/// </summary>
public class PerformanceTrackingResultDto
{
    /// <summary>
    /// Değerlendirici (dinleyici) performansları
    /// </summary>
    public List<EvaluatorPerformanceDto> EvaluatorPerformances { get; set; } = new();

    /// <summary>
    /// Firma kota durumları
    /// </summary>
    public List<CustomerQuotaStatusDto> CustomerQuotaStatuses { get; set; } = new();

    /// <summary>
    /// Proje tipi bazlı hedef karşılaştırmaları
    /// </summary>
    public List<ProjectTypePerformanceDto> ProjectTypePerformances { get; set; } = new();

    /// <summary>
    /// Genel özet
    /// </summary>
    public PerformanceSummaryDto Summary { get; set; } = new();
}

/// <summary>
/// Proje tipi bazlı performans karşılaştırma DTO
/// </summary>
public class ProjectTypePerformanceDto
{
    public int ProjectTypeId { get; set; }
    public string ProjectTypeName { get; set; } = string.Empty;

    // Hedefler (PerformanceSettings'den)
    public int? DailyTarget { get; set; }
    public int? WeeklyTarget { get; set; }
    public int? MonthlyTarget { get; set; }
    public int? YearlyTarget { get; set; }
    public decimal? SuccessThreshold { get; set; }
    public decimal? WarningThreshold { get; set; }

    // Gerçekleşen değerler
    public int TodayCount { get; set; }
    public int WeekCount { get; set; }
    public int MonthCount { get; set; }
    public int YearCount { get; set; }
    public decimal AverageScore { get; set; }

    // Hedef yüzdeleri
    public decimal? DailyPercentage { get; set; }
    public decimal? WeeklyPercentage { get; set; }
    public decimal? MonthlyPercentage { get; set; }
    public decimal? YearlyPercentage { get; set; }
}

/// <summary>
/// Değerlendirici (dinleyici) performans DTO
/// </summary>
public class EvaluatorPerformanceDto
{
    public int EvaluatorId { get; set; }
    public string EvaluatorName { get; set; } = string.Empty;
    public string? Department { get; set; }

    /// <summary>
    /// Bugün yapılan dinleme sayısı
    /// </summary>
    public int TodayCount { get; set; }

    /// <summary>
    /// Bu hafta yapılan dinleme sayısı
    /// </summary>
    public int WeekCount { get; set; }

    /// <summary>
    /// Bu ay yapılan dinleme sayısı
    /// </summary>
    public int MonthCount { get; set; }

    /// <summary>
    /// Bu yıl yapılan dinleme sayısı
    /// </summary>
    public int YearCount { get; set; }

    /// <summary>
    /// Toplam dinleme sayısı
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Ortalama puan
    /// </summary>
    public decimal AverageScore { get; set; }
}

/// <summary>
/// Firma kota durumu DTO
/// </summary>
public class CustomerQuotaStatusDto
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerCode { get; set; }

    /// <summary>
    /// Toplam hedef dinleme sayısı
    /// </summary>
    public int? TargetCount { get; set; }

    /// <summary>
    /// Günlük kota
    /// </summary>
    public int? DailyQuota { get; set; }

    /// <summary>
    /// Haftalık kota
    /// </summary>
    public int? WeeklyQuota { get; set; }

    /// <summary>
    /// Aylık kota
    /// </summary>
    public int? MonthlyQuota { get; set; }

    /// <summary>
    /// Bugün yapılan dinleme sayısı
    /// </summary>
    public int TodayCount { get; set; }

    /// <summary>
    /// Bu hafta yapılan dinleme sayısı
    /// </summary>
    public int WeekCount { get; set; }

    /// <summary>
    /// Bu ay yapılan dinleme sayısı
    /// </summary>
    public int MonthCount { get; set; }

    /// <summary>
    /// Toplam yapılan dinleme sayısı
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Hedef tamamlanma yüzdesi
    /// </summary>
    public decimal? TargetCompletionPercentage { get; set; }

    /// <summary>
    /// Günlük kota kullanım yüzdesi
    /// </summary>
    public decimal? DailyQuotaUsagePercentage { get; set; }

    /// <summary>
    /// Haftalık kota kullanım yüzdesi
    /// </summary>
    public decimal? WeeklyQuotaUsagePercentage { get; set; }

    /// <summary>
    /// Aylık kota kullanım yüzdesi
    /// </summary>
    public decimal? MonthlyQuotaUsagePercentage { get; set; }
}

/// <summary>
/// Performans özet DTO
/// </summary>
public class PerformanceSummaryDto
{
    /// <summary>
    /// Toplam değerlendirici sayısı
    /// </summary>
    public int TotalEvaluators { get; set; }

    /// <summary>
    /// Aktif firma sayısı
    /// </summary>
    public int TotalActiveCustomers { get; set; }

    /// <summary>
    /// Bugün toplam dinleme
    /// </summary>
    public int TotalTodayEvaluations { get; set; }

    /// <summary>
    /// Bu hafta toplam dinleme
    /// </summary>
    public int TotalWeekEvaluations { get; set; }

    /// <summary>
    /// Bu ay toplam dinleme
    /// </summary>
    public int TotalMonthEvaluations { get; set; }

    /// <summary>
    /// Bu yıl toplam dinleme
    /// </summary>
    public int TotalYearEvaluations { get; set; }
}

// ===== GENEL SORU PUAN DAĞILIMI DTO'LARI =====

/// <summary>
/// Genel soru puan dağılımı sonuç DTO
/// </summary>
public class SurveyQuestionScoreDistributionResultDto
{
    public List<SurveyQuestionScoreDistributionDto> Questions { get; set; } = new();
    public int TotalResponses { get; set; }
    public decimal OverallAverageScore { get; set; }
}

/// <summary>
/// Soru bazlı puan dağılımı DTO
/// </summary>
public class SurveyQuestionScoreDistributionDto
{
    public int QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? GroupName { get; set; }
    public int Order { get; set; }

    /// <summary>
    /// Bu soruya verilen toplam yanıt sayısı
    /// </summary>
    public int ResponseCount { get; set; }

    /// <summary>
    /// Ortalama puan (yüzde olarak)
    /// </summary>
    public decimal? AverageScore { get; set; }

    /// <summary>
    /// Maksimum alınabilecek puan
    /// </summary>
    public int MaxPoints { get; set; }

    /// <summary>
    /// Ortalama ham puan
    /// </summary>
    public decimal? AverageRawScore { get; set; }
}

// ===== PERSONEL SORU BAZLI PERFORMANS RAPORU DTO'LARI =====

/// <summary>
/// Personel Soru Bazlı Performans Raporu filtre DTO
/// </summary>
public class PersonnelQuestionPerformanceFilterDto
{
    public List<int>? CustomerIds { get; set; }
    public List<int>? ProjectIds { get; set; }
    public List<int>? OrganizationIds { get; set; }
    public List<int>? PersonnelIds { get; set; }
    public List<int>? PeriodIds { get; set; }
    public List<DateRangeFilter>? DateRanges { get; set; }
}

/// <summary>
/// Personel Soru Bazlı Performans Raporu satır DTO (Excel export için)
/// </summary>
public class PersonnelQuestionPerformanceDto
{
    public string ProjectName { get; set; } = string.Empty;
    public string PersonnelName { get; set; } = string.Empty;
    public int PersonnelId { get; set; }
    public string? Department { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public int Year { get; set; }
    public string PeriodMonth { get; set; } = string.Empty;
    public decimal AverageScore { get; set; }
    public int ErrorCount { get; set; }
    public int EvaluationCount { get; set; }
}

/// <summary>
/// Personel Soru Bazlı Performans Raporu - Tablo görünümü için ana DTO
/// </summary>
public class PersonnelQuestionPerformanceReportDto
{
    /// <summary>
    /// Kolon başlıkları (soru grup isimleri)
    /// </summary>
    public List<string> GroupNames { get; set; } = new();

    /// <summary>
    /// Personel satırları
    /// </summary>
    public List<PersonnelQuestionPerformanceRowDto> Rows { get; set; } = new();

    /// <summary>
    /// Toplam değerlendirme sayısı
    /// </summary>
    public int TotalEvaluations { get; set; }
}

/// <summary>
/// Personel Soru Bazlı Performans Raporu - Satır DTO
/// </summary>
public class PersonnelQuestionPerformanceRowDto
{
    public int PersonnelId { get; set; }
    public string PersonnelName { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? OrganizationName { get; set; }
    public int TotalEvaluations { get; set; }
    public decimal OverallAverage { get; set; }

    /// <summary>
    /// Her GroupName için puan bilgisi (sıralama GroupNames ile aynı)
    /// </summary>
    public List<GroupScoreDto> GroupScores { get; set; } = new();
}

/// <summary>
/// Grup bazlı puan bilgisi
/// </summary>
public class GroupScoreDto
{
    public string GroupName { get; set; } = string.Empty;
    public decimal? AverageScore { get; set; }
    public int EvaluationCount { get; set; }
    public int ErrorCount { get; set; }
}
