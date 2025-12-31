using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Evaluation;

public class SubmitEvaluationDto
{
    [Required]
    public int AssignmentId { get; set; }

    /// <summary>
    /// Değerlendirmenin ait olduğu dönem
    /// </summary>
    public int? AssignmentPeriodId { get; set; }

    public int? EvaluatorId { get; set; }

    [Required]
    public List<SubmitAnswerDto> Answers { get; set; } = new();

    public string? Notes { get; set; }

    // ===== YENİ ALANLAR - Çağrı Denetleme =====

    /// <summary>
    /// Denetim Yorumu
    /// </summary>
    public string? EvaluationComment { get; set; }

    /// <summary>
    /// Çağrı ID/numarası
    /// </summary>
    public string? CallId { get; set; }

    /// <summary>
    /// Çağrı tarihi
    /// </summary>
    public DateTime? CallDate { get; set; }

    /// <summary>
    /// Süre (dakika)
    /// </summary>
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Değerlendirilen personel ID
    /// </summary>
    public int? EvaluatedPersonnelId { get; set; }

    /// <summary>
    /// Tanımsız personel adı
    /// </summary>
    public string? EvaluatedUnknownPersonnel { get; set; }

    /// <summary>
    /// Kontrol tarihi
    /// </summary>
    public DateTime? ControlDate { get; set; }

    /// <summary>
    /// Kontrol saati
    /// </summary>
    public string? ControlTime { get; set; }

    /// <summary>
    /// Form açılma zamanı
    /// </summary>
    public DateTime? FormOpenedAt { get; set; }

    /// <summary>
    /// Taslak olarak kaydet
    /// </summary>
    public bool SaveAsDraft { get; set; } = false;
}

public class SubmitAnswerDto
{
    [Required]
    public int QuestionId { get; set; }

    public string? AnswerText { get; set; }

    public int? AnswerNumeric { get; set; }

    public bool IsNA { get; set; } = false;

    // ===== YENİ ALANLAR =====

    /// <summary>
    /// Verilen puan (ham puan)
    /// </summary>
    public decimal? GivenPoints { get; set; }

    /// <summary>
    /// Değerlendirici notu
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Öneri notu
    /// </summary>
    public string? RecommendationNotes { get; set; }

    /// <summary>
    /// Ceza uygulansın mı?
    /// </summary>
    public bool ApplyPenalty { get; set; } = false;

    /// <summary>
    /// Seçilen ceza tipi (None, YellowCard, RedCard)
    /// </summary>
    public string? SelectedPenaltyType { get; set; }
}

/// <summary>
/// Değerlendirme başlatma için DTO
/// </summary>
public class StartEvaluationDto
{
    [Required]
    public int AssignmentId { get; set; }

    /// <summary>
    /// Değerlendirmenin ait olduğu dönem
    /// </summary>
    public int? AssignmentPeriodId { get; set; }

    public int? EvaluatorId { get; set; }

    /// <summary>
    /// Çağrı ID/numarası
    /// </summary>
    public string? CallId { get; set; }

    /// <summary>
    /// Çağrı tarihi
    /// </summary>
    public DateTime? CallDate { get; set; }

    /// <summary>
    /// Değerlendirilen personel ID
    /// </summary>
    public int? EvaluatedPersonnelId { get; set; }

    /// <summary>
    /// Tanımsız personel adı
    /// </summary>
    public string? EvaluatedUnknownPersonnel { get; set; }
}

/// <summary>
/// Taslak güncelleme için DTO
/// </summary>
public class UpdateDraftDto
{
    [Required]
    public int EvaluationId { get; set; }

    public List<SubmitAnswerDto> Answers { get; set; } = new();

    public string? Notes { get; set; }

    public string? EvaluationComment { get; set; }
}
