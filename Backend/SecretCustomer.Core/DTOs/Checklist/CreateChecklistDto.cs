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
    /// Maksimum toplam puan (tüm soruların ağırlıklarının toplamı)
    /// </summary>
    public decimal MaxTotalPoints { get; set; } = 100;

    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(100)]
    public string? TemplateName { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Soru gruplarını gizle (formda grup isimleri görünmez, raporlamada kullanılır)
    /// </summary>
    public bool HideGroupNames { get; set; }

    /// <summary>
    /// Sorular - Direkt checklist'e bağlı, GroupName ile gruplandırılır
    /// </summary>
    public List<CreateQuestionDto> Questions { get; set; } = new();
}

public class CreateQuestionDto
{
    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? SelfText { get; set; }

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
    /// Maksimum puan (0'dan MaxPoints'e kadar butonlar)
    /// 1 = Evet/Hayır, 2+ = Likert ölçeği
    /// </summary>
    public int MaxPoints { get; set; } = 5;

    /// <summary>
    /// Ceza tipi (ScoringType=Penalty olduğunda): None, YellowCard, RedCard
    /// </summary>
    public string PenaltyType { get; set; } = "None";

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
    /// Soru grubu (opsiyonel) - Raporlama için gruplandırma
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// Alt Kriter seçim tipi: 1=Tekli, 2=Çoklu (varsayılan)
    /// </summary>
    public int SelectionTypeId { get; set; } = 2;

    /// <summary>
    /// Puan girişi gösterilsin mi? (Online anketler için false yapılabilir)
    /// </summary>
    public bool ShowScoreInput { get; set; } = true;

    /// <summary>
    /// Yorum yapılabilir mi? (Online anketler için false yapılabilir)
    /// </summary>
    public bool AllowComment { get; set; } = true;

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
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? SelfDescription { get; set; }

    public int Order { get; set; }

    /// <summary>
    /// Ağırlık puanı - Bu alt kriter seçildiğinde düşürülecek puan
    /// </summary>
    public decimal WeightPoints { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
