using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Kontrol Grubu (Section) - Kontrol listesindeki soru gruplarını temsil eder
/// </summary>
[ExcelTemplate("Bölüm", Description = "Kontrol listesi bölümleri için Excel import/export", IsAvailable = true)]
public class Section : BaseEntity
{
    public Guid ChecklistId { get; set; }
    public Checklist Checklist { get; set; } = null!;

    [ExcelColumn("Bölüm Adı", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Bölümün adı", SampleValue = "Hizmet Kalitesi")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Açıklama", 2, ColumnType = ExcelColumnType.Text,
        Description = "Bölüm hakkında açıklama", SampleValue = "Hizmet kalitesi ile ilgili sorular")]
    public string? Description { get; set; }

    [ExcelColumn("Sıra", 3, IsRequired = true, ColumnType = ExcelColumnType.Number,
        Description = "Bölümün sırası", SampleValue = "1")]
    public int Order { get; set; }

    // ===== YENİ ALANLAR - Kontrol Grubu Geliştirmeleri =====

    /// <summary>
    /// Grup puanlama tipi (Puanlı, Puansız, Cezalı)
    /// </summary>
    [ExcelColumn("Grup Tipi", 4, ColumnType = ExcelColumnType.Dropdown,
        Description = "Kontrol grubunun puanlama tipi",
        DropdownOptions = "[\"Scored\", \"Unscored\", \"Penalty\"]",
        SampleValue = "Scored")]
    public ScoringType GroupType { get; set; } = ScoringType.Scored;

    /// <summary>
    /// Grubun toplam ağırlık puanı
    /// </summary>
    [ExcelColumn("Ağırlık Puanı", 5, ColumnType = ExcelColumnType.Number,
        Description = "Grubun toplam ağırlık puanı", SampleValue = "20")]
    public decimal WeightPoints { get; set; } = 1;

    /// <summary>
    /// Grubun maksimum puanı
    /// </summary>
    [ExcelColumn("Maks Puan", 6, ColumnType = ExcelColumnType.Number,
        Description = "Gruptan alınabilecek maksimum puan", SampleValue = "100")]
    public decimal MaxPoints { get; set; } = 100;

    /// <summary>
    /// Grup aktif mi?
    /// </summary>
    [ExcelColumn("Aktif", 7, ColumnType = ExcelColumnType.Boolean,
        Description = "Grup aktif mi?", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
