namespace SecretCustomer.Core.DTOs.Checklist;

/// <summary>
/// Liste görünümü için hafif DTO (Questions yok)
/// </summary>
public class ChecklistListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsScored { get; set; }
    public bool IsActive { get; set; }
    public int Version { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ChecklistType { get; set; } = "CallPerformance";
    public string ScoringMethod { get; set; } = "Maximum";
    public string? Code { get; set; }
    public decimal MaxTotalPoints { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }
    public int QuestionCount { get; set; }
}

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

    public decimal MaxTotalPoints { get; set; } = 100;
    public string? Code { get; set; }
    public string? TemplateName { get; set; }
    public DateTime? ValidFrom { get; set; }
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Soru gruplarını gizle (formda grup isimleri görünmez, raporlamada kullanılır)
    /// </summary>
    public bool HideGroupNames { get; set; }

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
    /// Maksimum puan (0'dan MaxPoints'e kadar butonlar)
    /// </summary>
    public int MaxPoints { get; set; } = 5;

    /// <summary>
    /// Ceza tipi: None, YellowCard, RedCard
    /// </summary>
    public string PenaltyType { get; set; } = "None";
    public string PenaltyTypeName { get; set; } = "Yok";

    public bool IsRequired { get; set; }
    public string? RecommendedNote { get; set; }
    public string? HelpText { get; set; }

    /// <summary>
    /// Soru grubu (raporlama için)
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// Alt Kriter seçim tipi: 1=Tekli, 2=Çoklu
    /// </summary>
    public int SelectionTypeId { get; set; } = 2;

    /// <summary>
    /// Puan girişi gösterilsin mi?
    /// </summary>
    public bool ShowScoreInput { get; set; } = true;

    /// <summary>
    /// Yorum yapılabilir mi?
    /// </summary>
    public bool AllowComment { get; set; } = true;

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

/// <summary>
/// Kontrol listesi filtreleme
/// </summary>
public class ChecklistFilterDto
{
    // Filtreler
    public string? SearchText { get; set; }
    public bool IncludeInactive { get; set; }
}
