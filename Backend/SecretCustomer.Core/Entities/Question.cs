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

    [ExcelColumn("Soru Metni", 1, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Sorunun/kriterin tam metni", SampleValue = "Çağrı standartlarına uyum")]
    public string Text { get; set; } = string.Empty;

    [ExcelColumn("Sıra", 2, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Sorunun sırası", SampleValue = "1")]
    public int Order { get; set; }

    /// <summary>
    /// Puanlama tipi (Puanlı, Puansız, Cezalı)
    /// </summary>
    [ExcelColumn("Puanlama Tipi", 3, ColumnType = ExcelColumnTypes.Ids.Dropdown,
        Description = "Sorunun puanlama tipi",
        DropdownOptions = "[\"Scored\", \"Unscored\", \"Penalty\"]",
        SampleValue = "Scored")]
    public int ScoringTypeId { get; set; } = ScoringTypes.Ids.Scored;

    /// <summary>
    /// Ağırlık puanı - Sorunun toplam skora etkisi (varsayılan 10, 10 soru = 100 puan)
    /// </summary>
    [ExcelColumn("Ağırlık Puanı", 4, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Sorunun ağırlık puanı", SampleValue = "10")]
    public decimal WeightPoints { get; set; } = 10;

    /// <summary>
    /// Maksimum puan - Bu soru için verilebilecek maksimum puan (0'dan MaxPoints'e kadar butonlar)
    /// 1 = Evet/Hayır, 2+ = Likert ölçeği
    /// </summary>
    [ExcelColumn("Maks Puan", 5, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Maksimum puan değeri", SampleValue = "5")]
    public int MaxPoints { get; set; } = 5;

    /// <summary>
    /// Ceza tipi (Sarı Kart / Kırmızı Kart) - ScoringType=Penalty olduğunda kullanılır (PenaltyTypes.Ids kullanılır)
    /// </summary>
    [ExcelColumn("Ceza Tipi", 6, ColumnType = ExcelColumnTypes.Ids.Dropdown,
        Description = "Ceza tipi (Cezalı sorular için)",
        DropdownOptions = "[\"None\", \"YellowCard\", \"RedCard\"]",
        SampleValue = "None")]
    public int PenaltyTypeId { get; set; } = PenaltyTypes.Ids.None;

    [ExcelColumn("N/A İzni", 7, ColumnType = ExcelColumnTypes.Ids.Boolean,
        Description = "N/A seçeneğine izin verilir mi?", SampleValue = "false")]
    public bool AllowNA { get; set; } = false;

    [ExcelColumn("Zorunlu", 8, ColumnType = ExcelColumnTypes.Ids.Boolean,
        Description = "Soru zorunlu mu?", SampleValue = "true")]
    public bool IsRequired { get; set; } = true;

    /// <summary>
    /// Önerilen açıklama / Öneri notu
    /// </summary>
    [ExcelColumn("Önerilen Açıklama", 9, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Bu soru için önerilen açıklama/öneri")]
    public string? RecommendedNote { get; set; }

    /// <summary>
    /// Soru için yardımcı metin / ipucu
    /// </summary>
    [ExcelColumn("Yardımcı Metin", 10, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Değerlendirici için yardımcı metin")]
    public string? HelpText { get; set; }

    /// <summary>
    /// Soru grubu (opsiyonel) - Raporlama için gruplandırma
    /// </summary>
    [ExcelColumn("Grup", 11, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Sorunun ait olduğu grup (raporlama için)", SampleValue = "İletişim")]
    public string? GroupName { get; set; }

    /// <summary>
    /// Alt kriter seçim tipi (Tek Seçim / Çoklu Seçim)
    /// SubCriteria varsa bu ayara göre radio veya checkbox gösterilir
    /// </summary>
    public int SelectionTypeId { get; set; } = SelectionTypes.Ids.Multiple;

    /// <summary>
    /// Puan girişi gösterilsin mi?
    /// false ise sadece SubCriteria seçimi yapılır (anketler için)
    /// </summary>
    public bool ShowScoreInput { get; set; } = true;

    // Navigation properties
    public ICollection<Answer> Answers { get; set; } = new List<Answer>();
    public ICollection<QuestionAttachment> Attachments { get; set; } = new List<QuestionAttachment>();

    /// <summary>
    /// Alt Kriterler/Öneriler - Değerlendirme sırasında seçilebilir
    /// Örnek: "İlgili davranılmadı", "İsim ile hitap etmedi"
    /// </summary>
    public ICollection<QuestionSubCriteria> SubCriteria { get; set; } = new List<QuestionSubCriteria>();
}
