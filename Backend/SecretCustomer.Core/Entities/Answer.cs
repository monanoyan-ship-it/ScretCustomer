using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Değerlendirme cevabı - Her soru için verilen cevabı temsil eder
/// </summary>
[ExcelTemplate("Cevap", Description = "Anket cevapları için Excel import/export", IsAvailable = true)]
public class Answer : BaseEntity
{
    public int EvaluationId { get; set; }
    public Evaluation Evaluation { get; set; } = null!;

    public int QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    [ExcelColumn("Cevap (Metin)", 1, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Metin cevabı", SampleValue = "Çok memnunum")]
    public string? AnswerText { get; set; }

    [ExcelColumn("Cevap (Sayısal)", 2, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Sayısal cevap (Likert, Star için)", SampleValue = "5")]
    public int? AnswerNumeric { get; set; }

    [ExcelColumn("Kazanılan Puan", 3, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Bu cevap için kazanılan puan", SampleValue = "10")]
    public decimal? EarnedPoints { get; set; }

    // ===== YENİ ALANLAR - Değerlendirme Geliştirmeleri =====

    /// <summary>
    /// Verilen puan (ham puan)
    /// </summary>
    [ExcelColumn("Verilen Puan", 4, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Değerlendirici tarafından verilen puan", SampleValue = "8")]
    public decimal? GivenPoints { get; set; }

    /// <summary>
    /// Değerlendirici notu
    /// </summary>
    [ExcelColumn("Notlar", 5, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Değerlendirici notları")]
    public string? Notes { get; set; }

    /// <summary>
    /// Öneri notu (önerilen açıklama)
    /// </summary>
    [ExcelColumn("Öneri", 6, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Değerlendirici önerisi")]
    public string? RecommendationNotes { get; set; }

    /// <summary>
    /// Ek dosya yolu
    /// </summary>
    [ExcelColumn("Dosya", 7, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Ek dosya yolu")]
    public string? AttachmentPath { get; set; }

    /// <summary>
    /// Ek dosya adı
    /// </summary>
    public string? AttachmentFileName { get; set; }

    /// <summary>
    /// Ceza uygulandı mı? (Cezalı sorular için)
    /// </summary>
    [ExcelColumn("Ceza Uygulandı", 8, ColumnType = ExcelColumnTypes.Ids.Boolean,
        Description = "Ceza uygulandı mı?", SampleValue = "false")]
    public bool IsPenaltyApplied { get; set; } = false;

    /// <summary>
    /// Uygulanan ceza tipi (PenaltyTypes.Ids kullanılır)
    /// </summary>
    [ExcelColumn("Ceza Tipi", 9, ColumnType = ExcelColumnTypes.Ids.Dropdown,
        Description = "Uygulanan ceza tipi",
        DropdownOptions = "[\"None\", \"YellowCard\", \"RedCard\"]",
        SampleValue = "None")]
    public int AppliedPenaltyTypeId { get; set; } = PenaltyTypes.Ids.None;

    // Navigation - Bu cevap için seçilen alt kriterler
    public ICollection<AnswerSubCriteriaSelection> SubCriteriaSelections { get; set; } = new List<AnswerSubCriteriaSelection>();
}
