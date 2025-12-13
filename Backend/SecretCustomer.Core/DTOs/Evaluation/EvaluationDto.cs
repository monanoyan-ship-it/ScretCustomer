namespace SecretCustomer.Core.DTOs.Evaluation;

public class EvaluationDto
{
    public Guid Id { get; set; }
    public Guid AssignmentId { get; set; }
    public Guid? EvaluatorId { get; set; }
    public string? EvaluatorName { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal? TotalScore { get; set; }
    public decimal? MaxScore { get; set; }
    public decimal? ScorePercentage { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
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
    public Guid? EvaluatedPersonnelId { get; set; }

    /// <summary>
    /// Değerlendirilen personel adı
    /// </summary>
    public string? EvaluatedPersonnelName { get; set; }

    /// <summary>
    /// Tanımsız personel adı
    /// </summary>
    public string? EvaluatedUnknownPersonnel { get; set; }

    /// <summary>
    /// Sarı kart sayısı
    /// </summary>
    public int YellowCardCount { get; set; }

    /// <summary>
    /// Kırmızı kart sayısı
    /// </summary>
    public int RedCardCount { get; set; }

    /// <summary>
    /// Form açılma zamanı
    /// </summary>
    public DateTime? FormOpenedAt { get; set; }

    /// <summary>
    /// Kontrol tarihi
    /// </summary>
    public DateTime? ControlDate { get; set; }

    /// <summary>
    /// Kontrol saati
    /// </summary>
    public string? ControlTime { get; set; }

    // Assignment bilgileri
    public string? ProjectName { get; set; }
    public string? BranchName { get; set; }
    public string? ChecklistName { get; set; }

    public List<AnswerDto> Answers { get; set; } = new();
}

public class AnswerDto
{
    public Guid Id { get; set; }
    public Guid QuestionId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? QuestionType { get; set; }
    public string? AnswerText { get; set; }
    public int? AnswerNumeric { get; set; }
    public bool IsNA { get; set; }
    public decimal? EarnedPoints { get; set; }

    // ===== YENİ ALANLAR =====

    /// <summary>
    /// Verilen puan
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
    /// Ek dosya adı
    /// </summary>
    public string? AttachmentFileName { get; set; }

    /// <summary>
    /// Ceza uygulandı mı?
    /// </summary>
    public bool IsPenaltyApplied { get; set; }

    /// <summary>
    /// Uygulanan ceza tipi
    /// </summary>
    public string? AppliedPenaltyType { get; set; }

    // Soru bilgileri
    public int? SectionOrder { get; set; }
    public string? SectionName { get; set; }
    public int? QuestionOrder { get; set; }
    public decimal? MaxPoints { get; set; }
    public string? ScoringType { get; set; }
    public string? PenaltyType { get; set; }
    public decimal? PenaltyValue { get; set; }
    public string? HelpText { get; set; }
    public string? RecommendedNote { get; set; }
}
