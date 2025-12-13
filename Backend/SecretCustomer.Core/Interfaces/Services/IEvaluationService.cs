using SecretCustomer.Core.DTOs.Evaluation;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IEvaluationService
{
    Task<EvaluationDto?> GetByIdAsync(Guid id);
    Task<EvaluationDto?> GetByAssignmentIdAsync(Guid assignmentId);
    Task<IEnumerable<EvaluationDto>> GetByEvaluatorIdAsync(Guid evaluatorId);
    Task<EvaluationDto> SubmitEvaluationAsync(SubmitEvaluationDto dto);
    Task<EvaluationDto> StartEvaluationAsync(Guid assignmentId, Guid? evaluatorId);

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
    Task<EvaluationFormDto?> GetEvaluationFormAsync(Guid assignmentId);

    /// <summary>
    /// Mevcut değerlendirmeyi yükle (düzenleme için)
    /// </summary>
    Task<EvaluationFormDto?> GetExistingEvaluationFormAsync(Guid evaluationId);

    /// <summary>
    /// Tüm değerlendirmeleri getir (yönetici için)
    /// </summary>
    Task<IEnumerable<EvaluationDto>> GetAllAsync(int page = 1, int pageSize = 20);

    /// <summary>
    /// Proje bazlı değerlendirmeleri getir
    /// </summary>
    Task<IEnumerable<EvaluationDto>> GetByProjectIdAsync(Guid projectId);

    /// <summary>
    /// Kapatılmış değerlendirmeyi taslağa al (Admin yetkisi gerektirir)
    /// </summary>
    Task<EvaluationDto> RevertToDraftAsync(Guid evaluationId, Guid revertedByUserId, string? reason = null);

    /// <summary>
    /// Değerlendirmeyi iptal et
    /// </summary>
    Task<EvaluationDto> CancelEvaluationAsync(Guid evaluationId, Guid cancelledByUserId, string? reason = null);
}

/// <summary>
/// Değerlendirme formu DTO - Checklist bilgileriyle birlikte
/// </summary>
public class EvaluationFormDto
{
    public Guid AssignmentId { get; set; }
    public Guid? EvaluationId { get; set; }
    public string Status { get; set; } = "New";

    // Proje/Atama bilgileri
    public string ProjectName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public string? CustomerName { get; set; }

    // Checklist bilgileri
    public Guid ChecklistId { get; set; }
    public string ChecklistName { get; set; } = string.Empty;
    public string? ChecklistType { get; set; }
    public string? ScoringMethod { get; set; }
    public decimal MaxTotalPoints { get; set; }
    public int? EstimatedDurationMinutes { get; set; }

    // Çağrı bilgileri (mevcut değerlendirmeden)
    public string? CallId { get; set; }
    public DateTime? CallDate { get; set; }
    public int? DurationMinutes { get; set; }
    public Guid? EvaluatedPersonnelId { get; set; }
    public string? EvaluatedUnknownPersonnel { get; set; }
    public string? EvaluationComment { get; set; }

    // Personel listesi (değerlendirme için)
    public List<PersonnelOptionDto> AvailablePersonnel { get; set; } = new();

    // Bölümler ve sorular
    public List<EvaluationSectionDto> Sections { get; set; } = new();

    // Mevcut cevaplar (düzenleme için)
    public List<AnswerDto> ExistingAnswers { get; set; } = new();
}

public class PersonnelOptionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
}

public class EvaluationSectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public string? GroupType { get; set; }
    public decimal WeightPoints { get; set; }
    public decimal MaxPoints { get; set; }
    public List<EvaluationQuestionDto> Questions { get; set; } = new();
}

public class EvaluationQuestionDto
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Order { get; set; }
    public int Points { get; set; }
    public bool IsRequired { get; set; }
    public bool AllowNA { get; set; }
    public string? OptionsJson { get; set; }

    // Yeni alanlar
    public string? ScoringType { get; set; }
    public decimal WeightPoints { get; set; }
    public decimal MaxPoints { get; set; }
    public string? PenaltyType { get; set; }
    public decimal PenaltyValue { get; set; }
    public string? RecommendedNote { get; set; }
    public string? HelpText { get; set; }
}
