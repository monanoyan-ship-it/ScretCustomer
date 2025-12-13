using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Attributes;

namespace SecretCustomer.Core.Entities;

[ExcelTemplate("Soru", Description = "Anket soruları için Excel import/export", IsAvailable = true)]
public class Question : BaseEntity
{
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;

    [ExcelColumn("Soru Metni", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Sorunun tam metni", SampleValue = "Ürün kalitesinden memnun musunuz?")]
    public string Text { get; set; } = string.Empty;

    [ExcelColumn("Soru Tipi", 2, IsRequired = true, ColumnType = ExcelColumnType.Dropdown,
        Description = "Sorunun tipi",
        DropdownOptions = "[\"YesNo\", \"Rating\", \"Text\", \"MultipleChoice\"]",
        SampleValue = "Rating")]
    public QuestionType Type { get; set; }

    [ExcelColumn("Sıra", 3, IsRequired = true, ColumnType = ExcelColumnType.Number,
        Description = "Sorunun sırası", SampleValue = "1")]
    public int Order { get; set; }

    [ExcelColumn("Puan", 4, ColumnType = ExcelColumnType.Number,
        Description = "Sorunun puanı", SampleValue = "10")]
    public int Points { get; set; } = 0;

    [ExcelColumn("N/A İzni", 5, ColumnType = ExcelColumnType.Boolean,
        Description = "N/A seçeneğine izin verilir mi?", SampleValue = "false")]
    public bool AllowNA { get; set; } = false;

    [ExcelColumn("Zorunlu", 6, ColumnType = ExcelColumnType.Boolean,
        Description = "Soru zorunlu mu?", SampleValue = "true")]
    public bool IsRequired { get; set; } = true;

    [ExcelColumn("Seçenekler (JSON)", 7, ColumnType = ExcelColumnType.Text,
        Description = "Çoktan seçmeli sorular için seçenekler (JSON formatında)")]
    public string? OptionsJson { get; set; }

    // ===== YENİ ALANLAR - Kontrol Listesi Geliştirmeleri =====

    /// <summary>
    /// Puanlama tipi (Puanlı, Puansız, Cezalı)
    /// </summary>
    [ExcelColumn("Puanlama Tipi", 8, ColumnType = ExcelColumnType.Dropdown,
        Description = "Sorunun puanlama tipi",
        DropdownOptions = "[\"Scored\", \"Unscored\", \"Penalty\"]",
        SampleValue = "Scored")]
    public ScoringType ScoringType { get; set; } = ScoringType.Scored;

    /// <summary>
    /// Ağırlık puanı - Sorunun toplam skora etkisi
    /// </summary>
    [ExcelColumn("Ağırlık Puanı", 9, ColumnType = ExcelColumnType.Number,
        Description = "Sorunun ağırlık puanı", SampleValue = "10")]
    public decimal WeightPoints { get; set; } = 1;

    /// <summary>
    /// Maksimum alınabilecek puan
    /// </summary>
    [ExcelColumn("Maks Puan", 10, ColumnType = ExcelColumnType.Number,
        Description = "Alınabilecek maksimum puan", SampleValue = "100")]
    public decimal MaxPoints { get; set; } = 100;

    /// <summary>
    /// Ceza tipi (Sarı Kart / Kırmızı Kart) - ScoringType=Penalty olduğunda kullanılır
    /// </summary>
    [ExcelColumn("Ceza Tipi", 11, ColumnType = ExcelColumnType.Dropdown,
        Description = "Ceza tipi (Cezalı sorular için)",
        DropdownOptions = "[\"None\", \"YellowCard\", \"RedCard\"]",
        SampleValue = "None")]
    public PenaltyType PenaltyType { get; set; } = PenaltyType.None;

    /// <summary>
    /// Önerilen açıklama / Öneri notu
    /// </summary>
    [ExcelColumn("Önerilen Açıklama", 12, ColumnType = ExcelColumnType.Text,
        Description = "Bu soru için önerilen açıklama/öneri")]
    public string? RecommendedNote { get; set; }

    /// <summary>
    /// Soru için yardımcı metin / ipucu
    /// </summary>
    [ExcelColumn("Yardımcı Metin", 13, ColumnType = ExcelColumnType.Text,
        Description = "Değerlendirici için yardımcı metin")]
    public string? HelpText { get; set; }

    /// <summary>
    /// Cezalı sorularda ceza değeri (puan düşürme miktarı)
    /// </summary>
    [ExcelColumn("Ceza Değeri", 14, ColumnType = ExcelColumnType.Number,
        Description = "Ceza durumunda düşürülecek puan", SampleValue = "0")]
    public decimal PenaltyValue { get; set; } = 0;

    // Navigation properties
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<QuestionAttachment> Attachments { get; set; } = new List<QuestionAttachment>();
}
