using SecretCustomer.Core.DTOs.Evaluation;
using SecretCustomer.Core.DTOs.Report;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IEvaluationService
{
    Task<EvaluationDto?> GetByIdAsync(int id);
    Task<EvaluationDto?> GetByProjectIdSingleAsync(int projectId);
    Task<IEnumerable<EvaluationDto>> GetByEvaluatorIdAsync(int evaluatorId);
    Task<IEnumerable<EvaluationDto>> GetByEvaluatorCustomerPersonnelIdAsync(int customerPersonnelId);

    /// <summary>
    /// Temsilcinin kendisi hakkındaki değerlendirmeleri getirir (EvaluatedCustomerPersonnelId ile)
    /// CustomerOperator'ın kendi performansını görmesi için kullanılır
    /// </summary>
    Task<IEnumerable<EvaluationDto>> GetByEvaluatedCustomerPersonnelIdAsync(int customerPersonnelId, List<int>? projectIds = null, List<DateRangeFilter>? dateRanges = null, string? evaluationType = null);

    Task<EvaluationDto> SubmitEvaluationAsync(SubmitEvaluationDto dto);
    Task<EvaluationDto> StartEvaluationAsync(int projectId, int? evaluatorId);

    // ===== YENİ METOTLAR - Çağrı Denetleme =====

    /// <summary>
    /// Değerlendirmeyi başlat (genişletilmiş)
    /// </summary>
    Task<EvaluationDto> StartEvaluationAsync(StartEvaluationDto dto);

    /// <summary>
    /// Taslak olarak kaydet
    /// </summary>
    Task<EvaluationDto> SaveDraftAsync(SubmitEvaluationDto dto);

    /// <summary>
    /// Taslağı güncelle
    /// </summary>
    Task<EvaluationDto> UpdateDraftAsync(UpdateDraftDto dto);

    /// <summary>
    /// Değerlendirme formunu yükle (checklist bilgileriyle birlikte)
    /// assignmentId üzerinden projeyi ve checklist'i bulur
    /// </summary>
    Task<EvaluationFormDto?> GetEvaluationFormAsync(int assignmentId);

    /// <summary>
    /// Mevcut değerlendirmeyi yükle (düzenleme için)
    /// </summary>
    Task<EvaluationFormDto?> GetExistingEvaluationFormAsync(int evaluationId);

    /// <summary>
    /// Tüm değerlendirmeleri getir (yönetici için)
    /// </summary>
    Task<IEnumerable<EvaluationDto>> GetAllAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Proje bazlı değerlendirmeleri getir
    /// </summary>
    Task<IEnumerable<EvaluationDto>> GetByProjectIdAsync(int projectId);

    /// <summary>
    /// Kapatılmış değerlendirmeyi taslağa al (Admin yetkisi gerektirir)
    /// </summary>
    Task<EvaluationDto> RevertToDraftAsync(int evaluationId, int revertedByUserId, string? reason = null);

    /// <summary>
    /// Değerlendirmeyi iptal et
    /// </summary>
    Task<EvaluationDto> CancelEvaluationAsync(int evaluationId, int cancelledByUserId, string? reason = null);

    /// <summary>
    /// Organizasyona göre personel listesi getir
    /// </summary>
    Task<List<PersonnelOptionDto>> GetPersonnelByOrganizationAsync(int organizationId);

    /// <summary>
    /// Puan hesapla - Tek merkezi hesaplama noktası
    /// Kaydetmez, sadece hesaplayıp sonucu döndürür
    /// </summary>
    Task<ScoreCalculationResultDto> CalculateScoreAsync(CalculateScoreRequestDto request);

    /// <summary>
    /// Mevcut değerlendirmenin puanını yeniden hesapla ve kaydet
    /// Eski bug nedeniyle 0 kalan puanları düzeltmek için kullanılır
    /// </summary>
    Task<(bool Success, string Message)> RecalculateScoreAsync(int evaluationId);

    /// <summary>
    /// Geçmiş değerlendirmelerden benzersiz açıklamaları getirir (autocomplete için)
    /// </summary>
    Task<List<string>> GetPastDescriptionsAsync();

    /// <summary>
    /// Tüm değerlendirmeleri filtreli getirir (yönetici) - Çoklu filtre desteği
    /// </summary>
    Task<EvaluationListResultDto> GetAllFilteredAsync(
        List<int>? projectIds, List<int>? customerIds, List<int>? organizationIds,
        List<int>? evaluatorIds, List<int>? checklistIds, List<string>? statuses,
        DateTime? startDate, DateTime? endDate, int page, int pageSize);

    /// <summary>
    /// Değerlendirmenin ekli dosyalarını listeler
    /// </summary>
    Task<List<EvaluationAttachmentDto>> GetAttachmentsAsync(int evaluationId);

    /// <summary>
    /// Çağrı ID'nin müşteri için mevcut olup olmadığını kontrol eder
    /// </summary>
    Task<bool> CheckCallIdExistsAsync(string callId, int projectId, int? evaluationId);

    /// <summary>
    /// Aktif proje kontrolü (assignment üzerinden)
    /// </summary>
    Task<(bool Found, bool IsActive)> CheckProjectActiveForAssignmentAsync(int projectId);

    /// <summary>
    /// Answer ID'leri frontend'e dön (dosya yükleme için gerekli)
    /// </summary>
    Task<List<AnswerIdDto>> GetAnswerIdsAsync(int evaluationId);

    /// <summary>
    /// Taslağa alma talebi oluşturur (kullanıcı tarafından)
    /// </summary>
    Task<RevertRequestResultDto> CreateRevertRequestAsync(int evaluationId, int userId, string? userType, string? reason);

    /// <summary>
    /// Tamamlanmış değerlendirmelerin puanlarını toplu yeniden hesaplar (Admin only)
    /// </summary>
    Task<RecalculateScoresResultDto> RecalculateAllScoresAsync(int? projectId);

    /// <summary>
    /// Taslak değerlendirmeyi siler
    /// </summary>
    Task<DeleteDraftResultDto> DeleteDraftAsync(int evaluationId, int userId, string? userType, bool isAdmin);

    /// <summary>
    /// Evaluator bazlı filtreli değerlendirmeleri getirir
    /// </summary>
    Task<List<EvaluationDto>> GetByEvaluatorFilteredAsync(int userId, string? userType,
        string? status, string? search, string? personnel,
        DateTime? startDate, DateTime? endDate, DateTime? controlStartDate, DateTime? controlEndDate,
        List<int>? projectIds, List<string>? statuses, int? evaluationId = null);

    /// <summary>
    /// Sonradan bildirim maili gönderir (tamamlanmış değerlendirme için)
    /// </summary>
    Task SendNotificationAsync(int evaluationId);

    /// <summary>
    /// Excel export için değerlendirmeleri getirir (entity olarak, Excel oluşturma controller'da kalır)
    /// </summary>
    Task<List<EvaluationExportDto>> GetEvaluationsForExportAsync(int userId, string? userType,
        string? status, string? search, string? personnel,
        DateTime? startDate, DateTime? endDate, DateTime? controlStartDate, DateTime? controlEndDate,
        List<int>? projectIds, List<int>? customerIds, List<int>? organizationIds,
        List<int>? evaluatorIds, List<int>? checklistIds, List<string>? statuses,
        int? evaluationId = null);
}

// ===== Result DTOs =====

public class EvaluationListResultDto
{
    public List<EvaluationListItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class EvaluationListItemDto
{
    public int Id { get; set; }
    public int StatusId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProjectName { get; set; }
    public string? ChecklistName { get; set; }
    public string? EvaluatorName { get; set; }
    public string? PersonnelName { get; set; }
    public decimal? TotalScore { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? ScorePercentage { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class EvaluationAttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
}

public class AnswerIdDto
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
}

public class RevertRequestResultDto
{
    public bool Success { get; set; }
    public int? ApprovalId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? ErrorMessage { get; set; }
    public int? StatusCode { get; set; }
}

public class RecalculateScoresResultDto
{
    public int UpdatedCount { get; set; }
    public int ErrorCount { get; set; }
    public int TotalProcessed { get; set; }
}

public class DeleteDraftResultDto
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? StatusCode { get; set; }
}

public class EvaluationExportDto
{
    public string? CallId { get; set; }
    public DateTime? CallDate { get; set; }
    public string? CallTime { get; set; }
    public string? Duration { get; set; }
    public string? PersonnelName { get; set; }
    public string? ProjectName { get; set; }
    public string? ChecklistName { get; set; }
    public decimal? ScorePercentage { get; set; }
    public int YellowCardCount { get; set; }
    public int RedCardCount { get; set; }
    public int StatusId { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Değerlendirme formu DTO - Checklist bilgileriyle birlikte
/// </summary>
public class EvaluationFormDto
{
    public int ProjectId { get; set; }
    public int? AssignmentId { get; set; }
    public int? EvaluationId { get; set; }
    public int? CustomerDealerId { get; set; }
    public string Status { get; set; } = "New";

    // Proje/Atama bilgileri
    public string ProjectName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }

    // Checklist bilgileri
    public int ChecklistId { get; set; }
    public string ChecklistName { get; set; } = string.Empty;
    public string? ChecklistType { get; set; }
    public string? ScoringMethod { get; set; }
    public decimal MaxTotalPoints { get; set; }

    // Çağrı bilgileri (mevcut değerlendirmeden)
    public string? CallId { get; set; }
    public DateTime? CallDate { get; set; }
    public string? CallTime { get; set; }
    public string? Duration { get; set; }
    public List<string> Descriptions { get; set; } = new();
    public int? EvaluatedPersonnelId { get; set; }
    public string? EvaluatedUnknownPersonnel { get; set; }
    public string? EvaluationComment { get; set; }

    // Organizasyon listesi (değerlendirme için - ZORUNLU seçim)
    public List<OrganizationOptionDto> AvailableOrganizations { get; set; } = new();

    // Seçili organizasyon
    public int? SelectedOrganizationId { get; set; }

    // Personel listesi (organizasyon seçildikten sonra doldurulur)
    public List<PersonnelOptionDto> AvailablePersonnel { get; set; } = new();

    // Dönem bilgileri
    public int? SelectedPeriodId { get; set; }
    public List<PeriodOptionDto> AvailablePeriods { get; set; } = new();

    /// <summary>
    /// Müşterinin PerEvaluation bildirim kuralı var mı?
    /// </summary>
    public bool HasPerEvaluationNotification { get; set; }

    // Ceza tipine göre soru grupları (Sorular, Sarı Kartlar, Kırmızı Kartlar)
    public List<PenaltyGroupDto> PenaltyGroups { get; set; } = new();

    // Mevcut cevaplar (düzenleme için)
    public List<AnswerDto> ExistingAnswers { get; set; } = new();
}

public class OrganizationOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Code { get; set; }
    public int Level { get; set; }
    public int PersonnelCount { get; set; }
}

public class PersonnelOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
    public int? OrganizationId { get; set; }
}

public class PeriodOptionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = "Open";
    public int TargetCount { get; set; }
    public int CompletedCount { get; set; }
}

public class PenaltyGroupDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? PenaltyType { get; set; }
    public decimal WeightPoints { get; set; }
    public decimal MaxPoints { get; set; }
    public List<EvaluationQuestionDto> Questions { get; set; } = new();
}

public class EvaluationQuestionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsRequired { get; set; }

    /// <summary>
    /// Puanlama tipi: Scored, Unscored, Penalty
    /// </summary>
    public string ScoringType { get; set; } = "Scored";

    /// <summary>
    /// Ağırlık puanı
    /// </summary>
    public decimal WeightPoints { get; set; }

    /// <summary>
    /// Maksimum puan (0'dan MaxPoints'e kadar butonlar)
    /// </summary>
    public int MaxPoints { get; set; } = 5;

    public string? PenaltyType { get; set; }
    public string? RecommendedNote { get; set; }
    public string? HelpText { get; set; }

    /// <summary>
    /// Yorum alanı gösterilsin mi
    /// </summary>
    public bool AllowComment { get; set; }

    /// <summary>
    /// Alt Kriterler/Öneriler
    /// </summary>
    public List<EvaluationSubCriteriaDto>? SubCriteria { get; set; }
}

public class EvaluationSubCriteriaDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public decimal WeightPoints { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Puan hesaplama isteği
/// </summary>
public class CalculateScoreRequestDto
{
    /// <summary>
    /// Checklist ID - Soruları almak için
    /// </summary>
    public int ChecklistId { get; set; }

    /// <summary>
    /// Cevaplar
    /// </summary>
    public List<ScoreAnswerDto> Answers { get; set; } = new();
}

/// <summary>
/// Puan hesaplama için cevap
/// </summary>
public class ScoreAnswerDto
{
    public int QuestionId { get; set; }

    /// <summary>
    /// Soru dahil mi? (Zorunlu olmayan sorular için)
    /// false ise bu soru hesaplamaya katılmaz
    /// </summary>
    public bool IsIncluded { get; set; } = true;

    /// <summary>
    /// Verilen puan (0-MaxPoints arası) - Maximum modu için
    /// </summary>
    public decimal? GivenPoints { get; set; }

    /// <summary>
    /// Seçilen SubCriteria ID'leri - CriteriaTotal modu için
    /// </summary>
    public List<int>? SelectedSubCriteriaIds { get; set; }

    /// <summary>
    /// Ceza uygulandı mı? (Penalty sorular için)
    /// </summary>
    public bool ApplyPenalty { get; set; }

    /// <summary>
    /// Seçilen ceza tipi: "YellowCard" veya "RedCard"
    /// </summary>
    public string? SelectedPenaltyType { get; set; }
}

/// <summary>
/// Puan hesaplama sonucu
/// </summary>
public class ScoreCalculationResultDto
{
    /// <summary>
    /// Kazanılan toplam puan
    /// </summary>
    public decimal TotalEarned { get; set; }

    /// <summary>
    /// Maksimum alınabilecek puan (dahil edilen sorulara göre)
    /// </summary>
    public decimal MaxPossible { get; set; }

    /// <summary>
    /// Yüzdelik puan (0-100)
    /// </summary>
    public decimal Percentage { get; set; }

    /// <summary>
    /// Sarı kart sayısı
    /// </summary>
    public int YellowCardCount { get; set; }

    /// <summary>
    /// Kırmızı kart sayısı
    /// </summary>
    public int RedCardCount { get; set; }

    /// <summary>
    /// Hesaplamaya dahil edilen soru sayısı
    /// </summary>
    public int IncludedQuestionCount { get; set; }

    /// <summary>
    /// Toplam soru sayısı (dahil edilmeyenler dahil)
    /// </summary>
    public int TotalQuestionCount { get; set; }
}
