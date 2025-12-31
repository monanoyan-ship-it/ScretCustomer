using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Checklist;

public class CreateChecklistDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public bool IsScored { get; set; } = true;

    // Kontrol listesi tipi
    public string ChecklistType { get; set; } = "CallPerformance";

    // Puanlama yöntemi (Maximum, Average, WeightedAverage, Sum)
    public string ScoringMethod { get; set; } = "Maximum";

    /// <summary>
    /// Likert Ölçeği - Puanlama için maksimum değer (örn: 5 = 0-5 arası)
    /// </summary>
    public int LikertScale { get; set; } = 5;

    /// <summary>
    /// Maksimum toplam puan (tüm soruların ağırlıklarının toplamı)
    /// </summary>
    public decimal MaxTotalPoints { get; set; } = 100;

    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(100)]
    public string? TemplateName { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int? EstimatedDurationMinutes { get; set; }

    // Firma ve Organizasyon
    public int? CustomerId { get; set; }
    public int? CustomerOrganizationId { get; set; }

    /// <summary>
    /// Sorular - Direkt checklist'e bağlı (Section yok!)
    /// </summary>
    public List<CreateQuestionDto> Questions { get; set; } = new();
}

public class CreateQuestionDto
{
    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    public int Order { get; set; }

    /// <summary>
    /// Puanlama tipi: Scored (Puanlı), Unscored (Puansız), Penalty (Cezalı)
    /// </summary>
    public string ScoringType { get; set; } = "Scored";

    /// <summary>
    /// Ağırlık puanı - Bu sorunun toplam skora etkisi (örn: 10)
    /// </summary>
    public decimal WeightPoints { get; set; } = 1;

    /// <summary>
    /// Kırılım sayısı / Ölçek - Bu soru kaç kırılımlı? (1, 2, 3, 4)
    /// 1 = Evet/Hayır (0 veya tam puan)
    /// 2 = İki kırılım (0, yarım, tam)
    /// 3 = Üç kırılım
    /// 4 = Dört kırılım (0, 1/4, 2/4, 3/4, tam)
    /// </summary>
    public int ScaleSteps { get; set; } = 4;

    /// <summary>
    /// Ceza tipi (ScoringType=Penalty olduğunda): None, YellowCard, RedCard
    /// </summary>
    public string PenaltyType { get; set; } = "None";

    /// <summary>
    /// Ceza değeri - Cezalı sorularda düşürülecek puan
    /// </summary>
    public decimal PenaltyValue { get; set; } = 0;

    /// <summary>
    /// N/A (Uygulanamaz) seçeneği izni
    /// </summary>
    public bool AllowNA { get; set; } = false;

    /// <summary>
    /// Zorunlu mu?
    /// </summary>
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Önerilen açıklama / Öneri notu
    /// </summary>
    [MaxLength(2000)]
    public string? RecommendedNote { get; set; }

    /// <summary>
    /// Yardımcı metin / ipucu
    /// </summary>
    [MaxLength(1000)]
    public string? HelpText { get; set; }

    /// <summary>
    /// Alt Kriterler/Öneriler - "İlgili davranılmadı", "İsim ile hitap etmedi" gibi
    /// </summary>
    public List<CreateSubCriteriaDto>? SubCriteria { get; set; }
}

/// <summary>
/// Alt kriter/öneri oluşturma DTO'su
/// </summary>
public class CreateSubCriteriaDto
{
    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public int Order { get; set; }

    /// <summary>
    /// Ağırlık puanı - Bu alt kriter seçildiğinde düşürülecek puan
    /// </summary>
    public decimal WeightPoints { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
