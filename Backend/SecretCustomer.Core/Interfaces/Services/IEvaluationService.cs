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
    public int? EstimatedDurationMinutes { get; set; }

    // Çağrı bilgileri (mevcut değerlendirmeden)
    public string? CallId { get; set; }
    public DateTime? CallDate { get; set; }
    public int? DurationMinutes { get; set; }
    public int? EvaluatedPersonnelId { get; set; }
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
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Title { get; set; }
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
