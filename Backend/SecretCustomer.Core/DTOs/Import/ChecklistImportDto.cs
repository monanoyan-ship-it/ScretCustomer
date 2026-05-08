namespace SecretCustomer.Core.DTOs.Import;

/// <summary>
/// CSV'den okunan checklist soru verisi
/// </summary>
public class ChecklistQuestionImportDto
{
    /// <summary>
    /// Soru grubu adı (raporlama için)
    /// </summary>
    public string GroupName { get; set; } = string.Empty;

    /// <summary>
    /// Soru metni
    /// </summary>
    public string QuestionText { get; set; } = string.Empty;

    /// <summary>
    /// Ağırlık puanı
    /// </summary>
    public decimal WeightPoints { get; set; } = 1;

    /// <summary>
    /// Maksimum puan (1-10)
    /// </summary>
    public int MaxPoints { get; set; } = 5;

    /// <summary>
    /// Puanlama tipi: Scored, Unscored, Penalty
    /// </summary>
    public string ScoringType { get; set; } = "Scored";

    /// <summary>
    /// Ceza tipi: None, YellowCard, RedCard
    /// </summary>
    public string PenaltyType { get; set; } = "None";

    /// <summary>
    /// Alt kriterler (pipe | ile ayrılmış)
    /// </summary>
    public string SubCriteria { get; set; } = string.Empty;

    /// <summary>
    /// Alt kriter ağırlık puanları (SubCriteria ile aynı sıra, | ile; örn. Likert 5→1: "5|4|3|2|1")
    /// </summary>
    public string SubCriteriaWeights { get; set; } = string.Empty;

    /// <summary>
    /// Sıra numarası (opsiyonel, verilmezse otomatik atanır)
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    /// Zorunlu mu?
    /// </summary>
    public bool IsRequired { get; set; } = false;

    /// <summary>
    /// Yardımcı metin
    /// </summary>
    public string HelpText { get; set; } = string.Empty;
}

/// <summary>
/// Checklist import işlemi sonucu
/// </summary>
public class ChecklistImportResultDto
{
    public bool Success { get; set; }
    public int TotalRows { get; set; }
    public int QuestionsCreated { get; set; }
    public int SubCriteriaCreated { get; set; }
    public int ChecklistId { get; set; }
    public string ChecklistName { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<ImportedQuestionInfo> ImportedQuestions { get; set; } = new();
}

/// <summary>
/// Import edilen soru bilgisi
/// </summary>
public class ImportedQuestionInfo
{
    public int Id { get; set; }
    public int Order { get; set; }
    public string GroupName { get; set; } = string.Empty;
    public string QuestionText { get; set; } = string.Empty;
    public decimal WeightPoints { get; set; }
    public int MaxPoints { get; set; }
    public string ScoringType { get; set; } = string.Empty;
    public int SubCriteriaCount { get; set; }
}
