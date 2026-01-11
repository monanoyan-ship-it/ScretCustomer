namespace SecretCustomer.Core.DTOs.Evaluation;

public class EvaluationDto
{
    public int Id { get; set; }
    public int AssignmentId { get; set; }
    public int? AssignmentPeriodId { get; set; }
    public string? AssignmentPeriodName { get; set; }
    public int? EvaluatorId { get; set; }
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
    /// Çağrı saati (HH:mm formatında)
    /// </summary>
    public string? CallTime { get; set; }

    /// <summary>
    /// Süre (dk:sn formatında, örn: "12:26")
    /// </summary>
    public string? Duration { get; set; }

    /// <summary>
    /// Açıklamalar listesi
    /// </summary>
    public List<string> Descriptions { get; set; } = new();

    /// <summary>
    /// Değerlendirilen personel ID
    /// </summary>
    public int? EvaluatedPersonnelId { get; set; }

    /// <summary>
    /// Değerlendirilen personel adı
    /// </summary>
    public string? EvaluatedPersonnelName { get; set; }

    /// <summary>
    /// Müşteri firma adı
    /// </summary>
    public string? CustomerName { get; set; }

    /// <summary>
    /// Organizasyon adı (virgülle ayrılmış liste)
    /// </summary>
    public string? OrganizationName { get; set; }

    /// <summary>
    /// Müşteri Yöneticisi adı (virgülle ayrılmış liste)
    /// </summary>
    public string? SupervisorName { get; set; }

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
    public string? ChecklistName { get; set; }
    public string? AssigneeName { get; set; }

    public List<AnswerDto> Answers { get; set; } = new();
}

public class AnswerDto
{
    public int Id { get; set; }
    public int QuestionId { get; set; }
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
    public string? GroupName { get; set; }
    public int? QuestionOrder { get; set; }

    /// <summary>
    /// Sorunun maksimum puanı (örn: 5 = 1-5 arası seçim)
    /// </summary>
    public int? QuestionMaxPoints { get; set; }

    /// <summary>
    /// Sorunun ağırlık puanı
    /// </summary>
    public decimal? WeightPoints { get; set; }

    /// <summary>
    /// DEPRECATED: WeightPoints yerine kullanılıyordu, geriye uyumluluk için
    /// </summary>
    public decimal? MaxPoints { get; set; }

    public string? ScoringType { get; set; }
    public string? PenaltyType { get; set; }
    public string? HelpText { get; set; }
    public string? RecommendedNote { get; set; }

    /// <summary>
    /// Seçilen alt kriter ID'leri
    /// </summary>
    public List<int>? SelectedSubCriteriaIds { get; set; }

    /// <summary>
    /// Seçilen alt kriter açıklamaları (görüntüleme için)
    /// </summary>
    public List<string>? SelectedSubCriteria { get; set; }
}
