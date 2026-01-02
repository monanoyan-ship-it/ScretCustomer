using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Attributes;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Soru (Kriter) - Değerlendirme formundaki her bir kriter
/// Örnek: "Çağrı standartlarına uyum", "Aktif dinleme" vb.
/// </summary>
[ExcelTemplate("Soru", Description = "Değerlendirme kriterleri için Excel import/export", IsAvailable = true)]
public class Question : BaseEntity
{
    /// <summary>
    /// Kontrol Listesi ID - Soru direkt checklist'e bağlı
    /// </summary>
    public int ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    /// <summary>
    /// Section ID - Geriye uyumluluk için (deprecated, ileride kaldırılacak)
    /// </summary>
    public int? SectionId { get; set; }
    public Section? Section { get; set; }

    [ExcelColumn("Soru Metni", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Sorunun/kriterin tam metni", SampleValue = "Çağrı standartlarına uyum")]
    public string Text { get; set; } = string.Empty;

    [ExcelColumn("Sıra", 2, IsRequired = true, ColumnType = ExcelColumnType.Number,
        Description = "Sorunun sırası", SampleValue = "1")]
    public int Order { get; set; }

    /// <summary>
    /// Puanlama tipi (Puanlı, Puansız, Cezalı)
    /// </summary>
    [ExcelColumn("Puanlama Tipi", 3, ColumnType = ExcelColumnType.Dropdown,
        Description = "Sorunun puanlama tipi",
        DropdownOptions = "[\"Scored\", \"Unscored\", \"Penalty\"]",
        SampleValue = "Scored")]
    public ScoringType ScoringType { get; set; } = ScoringType.Scored;

    /// <summary>
    /// Ağırlık puanı - Sorunun toplam skora etkisi (varsayılan 10, 10 soru = 100 puan)
    /// </summary>
    [ExcelColumn("Ağırlık Puanı", 4, ColumnType = ExcelColumnType.Number,
        Description = "Sorunun ağırlık puanı", SampleValue = "10")]
    public decimal WeightPoints { get; set; } = 10;

    /// <summary>
    /// Maksimum puan - Bu soru için verilebilecek maksimum puan (0'dan MaxPoints'e kadar butonlar)
    /// 1 = Evet/Hayır, 2+ = Likert ölçeği
    /// </summary>
    [ExcelColumn("Maks Puan", 5, ColumnType = ExcelColumnType.Number,
        Description = "Maksimum puan değeri", SampleValue = "5")]
    public int MaxPoints { get; set; } = 5;

    /// <summary>
    /// Ceza tipi (Sarı Kart / Kırmızı Kart) - ScoringType=Penalty olduğunda kullanılır
    /// </summary>
    [ExcelColumn("Ceza Tipi", 6, ColumnType = ExcelColumnType.Dropdown,
        Description = "Ceza tipi (Cezalı sorular için)",
        DropdownOptions = "[\"None\", \"YellowCard\", \"RedCard\"]",
        SampleValue = "None")]
    public PenaltyType PenaltyType { get; set; } = PenaltyType.None;

    [ExcelColumn("N/A İzni", 7, ColumnType = ExcelColumnType.Boolean,
        Description = "N/A seçeneğine izin verilir mi?", SampleValue = "false")]
    public bool AllowNA { get; set; } = false;

    [ExcelColumn("Zorunlu", 8, ColumnType = ExcelColumnType.Boolean,
        Description = "Soru zorunlu mu?", SampleValue = "true")]
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Önerilen açıklama / Öneri notu
    /// </summary>
    [ExcelColumn("Önerilen Açıklama", 9, ColumnType = ExcelColumnType.Text,
        Description = "Bu soru için önerilen açıklama/öneri")]
    public string? RecommendedNote { get; set; }

    /// <summary>
    /// Soru için yardımcı metin / ipucu
    /// </summary>
    [ExcelColumn("Yardımcı Metin", 10, ColumnType = ExcelColumnType.Text,
        Description = "Değerlendirici için yardımcı metin")]
    public string? HelpText { get; set; }

    // Navigation properties
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<QuestionAttachment> Attachments { get; set; } = new List<QuestionAttachment>();

    /// <summary>
    /// Alt Kriterler/Öneriler - Değerlendirme sırasında seçilebilir
    /// Örnek: "İlgili davranılmadı", "İsim ile hitap etmedi"
    /// </summary>
    public ICollection<QuestionSubCriteria> SubCriteria { get; set; } = new List<QuestionSubCriteria>();
}
