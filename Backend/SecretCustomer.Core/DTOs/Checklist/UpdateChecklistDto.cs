using System.ComponentModel.DataAnnotations;

namespace SecretCustomer.Core.DTOs.Checklist;

public class UpdateChecklistDto
{
    [Required]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public bool IsScored { get; set; }

    public bool IsActive { get; set; }

    // Kontrol listesi tipi
    public string ChecklistType { get; set; } = "CallPerformance";

    // Puanlama yöntemi
    public string ScoringMethod { get; set; } = "Maximum";

    public decimal MaxTotalPoints { get; set; } = 100;

    [MaxLength(50)]
    public string? Code { get; set; }

    [MaxLength(100)]
    public string? TemplateName { get; set; }

    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    // Firma ve Organizasyon
    public int? CustomerId { get; set; }
    public int? CustomerOrganizationId { get; set; }

    /// <summary>
    /// Sorular - Direkt checklist'e bağlı (Section yok!)
    /// </summary>
    public List<UpdateQuestionDto> Questions { get; set; } = new();
}

public class UpdateQuestionDto
{
    public int? Id { get; set; }

    [Required]
    [MaxLength(1000)]
    public string Text { get; set; } = string.Empty;

    public int Order { get; set; }

    /// <summary>
    /// Puanlama tipi: Scored (Puanlı), Unscored (Puansız), Penalty (Cezalı)
    /// </summary>
    public string ScoringType { get; set; } = "Scored";

    /// <summary>
    /// Ağırlık puanı - Bu sorunun toplam skora etkisi (varsayılan 10)
    /// </summary>
    public decimal WeightPoints { get; set; } = 10;

    /// <summary>
    /// Kırılım sayısı / Ölçek - Bu soru kaç kırılımlı? (1, 2, 3, 4)
    /// </summary>
    public int ScaleSteps { get; set; } = 4;

    /// <summary>
    /// Ceza tipi: None, YellowCard, RedCard
    /// </summary>
    public string PenaltyType { get; set; } = "None";

    public decimal PenaltyValue { get; set; } = 0;

    public bool AllowNA { get; set; }

    public bool IsRequired { get; set; }

    [MaxLength(2000)]
    public string? RecommendedNote { get; set; }

    [MaxLength(1000)]
    public string? HelpText { get; set; }

    /// <summary>
    /// Alt Kriterler/Öneriler
    /// </summary>
    public List<UpdateSubCriteriaDto>? SubCriteria { get; set; }
}

/// <summary>
/// Alt kriter/öneri güncelleme DTO'su
/// </summary>
public class UpdateSubCriteriaDto
{
    public int? Id { get; set; }

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    public int Order { get; set; }

    public decimal WeightPoints { get; set; } = 1;

    public bool IsActive { get; set; } = true;
}
