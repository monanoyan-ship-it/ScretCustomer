using SecretCustomer.Core.DTOs.Evaluation;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IEvaluationService
{
    Task<EvaluationDto?> GetByIdAsync(int id);
    Task<EvaluationDto?> GetByAssignmentIdAsync(int assignmentId);
    Task<IEnumerable<EvaluationDto>> GetByEvaluatorIdAsync(int evaluatorId);
    Task<EvaluationDto> SubmitEvaluationAsync(SubmitEvaluationDto dto);
    Task<EvaluationDto> StartEvaluationAsync(int assignmentId, int? evaluatorId);

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
}

/// <summary>
/// Değerlendirme formu DTO - Checklist bilgileriyle birlikte
/// </summary>
public class EvaluationFormDto
{
    public int AssignmentId { get; set; }
    public int? EvaluationId { get; set; }
    public string Status { get; set; } = "New";

    // Proje/Atama bilgileri
    public string ProjectName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
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
    public int? DurationMinutes { get; set; }
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

    // Bölümler ve sorular
    public List<EvaluationSectionDto> Sections { get; set; } = new();

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

public class EvaluationSectionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? GroupType { get; set; }
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
    public bool AllowNA { get; set; }

    /// <summary>
    /// Puanlama tipi: Scored, Unscored, Penalty
    /// </summary>
    public string ScoringType { get; set; } = "Scored";

    /// <summary>
    /// Ağırlık puanı
    /// </summary>
    public decimal WeightPoints { get; set; }

    /// <summary>
    /// Kırılım sayısı / Ölçek (1, 2, 3, 4)
    /// </summary>
    public int ScaleSteps { get; set; } = 4;

    public string? PenaltyType { get; set; }
    public decimal PenaltyValue { get; set; }
    public string? RecommendedNote { get; set; }
    public string? HelpText { get; set; }

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
