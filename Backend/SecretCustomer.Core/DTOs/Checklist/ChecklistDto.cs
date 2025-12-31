namespace SecretCustomer.Core.DTOs.Checklist;

public class ChecklistDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsScored { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }

    // Kontrol listesi tipi
    public string ChecklistType { get; set; } = "CallPerformance";
    public string ChecklistTypeName { get; set; } = "Çağrı Performans";

    // Puanlama yöntemi
    public string ScoringMethod { get; set; } = "Maximum";
    public string ScoringMethodName { get; set; } = "Maksimum";

    /// <summary>
    /// Likert Ölçeği - Puanlama için maksimum değer
    /// </summary>
    public int LikertScale { get; set; } = 5;

    public decimal MaxTotalPoints { get; set; } = 100;
    public string? Code { get; set; }
    public string? TemplateName { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int? EstimatedDurationMinutes { get; set; }

    // Firma ve Organizasyon
    public int? CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public int? CustomerOrganizationId { get; set; }
    public string? CustomerOrganizationName { get; set; }

    /// <summary>
    /// Sorular - Direkt checklist'e bağlı
    /// </summary>
    public List<QuestionDto> Questions { get; set; } = new();

    // İstatistikler
    public int QuestionCount { get; set; }
}

public class QuestionDto
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int Order { get; set; }

    /// <summary>
    /// Puanlama tipi: Scored, Unscored, Penalty
    /// </summary>
    public string ScoringType { get; set; } = "Scored";
    public string ScoringTypeName { get; set; } = "Puanlı";

    /// <summary>
    /// Ağırlık puanı (varsayılan 10, 10 soru = 100 puan)
    /// </summary>
    public decimal WeightPoints { get; set; } = 10;

    /// <summary>
    /// Kırılım sayısı / Ölçek (1, 2, 3, 4)
    /// </summary>
    public int ScaleSteps { get; set; } = 4;

    /// <summary>
    /// Ceza tipi: None, YellowCard, RedCard
    /// </summary>
    public string PenaltyType { get; set; } = "None";
    public string PenaltyTypeName { get; set; } = "Yok";

    public decimal PenaltyValue { get; set; } = 0;
    public bool AllowNA { get; set; }
    public bool IsRequired { get; set; }
    public string? RecommendedNote { get; set; }
    public string? HelpText { get; set; }

    /// <summary>
    /// Alt Kriterler/Öneriler
    /// </summary>
    public List<SubCriteriaDto>? SubCriteria { get; set; }
}

/// <summary>
/// Alt kriter/öneri response DTO'su
/// </summary>
public class SubCriteriaDto
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; }
    public decimal WeightPoints { get; set; } = 1;
    public bool IsActive { get; set; } = true;
}
