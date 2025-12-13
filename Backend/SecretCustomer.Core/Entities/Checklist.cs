using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Kontrol Listesi - Değerlendirme formlarının şablonunu temsil eder
/// </summary>
[ExcelTemplate("Kontrol Listesi", Description = "Kontrol listeleri için Excel import/export", IsAvailable = true)]
public class Checklist : BaseEntity
{
    [ExcelColumn("Kontrol Listesi Adı", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Kontrol listesinin adı", SampleValue = "Müşteri Memnuniyeti Anketi")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Açıklama", 2, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Kontrol listesi hakkında açıklama", SampleValue = "Genel müşteri memnuniyeti değerlendirmesi")]
    public string Description { get; set; } = string.Empty;

    [ExcelColumn("Puanlı", 3, ColumnType = ExcelColumnType.Boolean,
        Description = "Puanlı mı puansız mı?", SampleValue = "true")]
    public bool IsScored { get; set; } = true;

    [ExcelColumn("Aktif", 4, ColumnType = ExcelColumnType.Boolean,
        Description = "Kontrol listesi aktif mi?", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    [ExcelColumn("Versiyon", 5, ColumnType = ExcelColumnType.Number,
        Description = "Kontrol listesi versiyonu", SampleValue = "1")]
    public int Version { get; set; } = 1;

    // ===== YENİ ALANLAR - Kontrol Listesi Geliştirmeleri =====

    /// <summary>
    /// Kontrol listesi tipi (Çağrı Performans, Fiziksel Denetim vb.)
    /// </summary>
    [ExcelColumn("K.L. Tipi", 6, ColumnType = ExcelColumnType.Dropdown,
        Description = "Kontrol listesi tipi",
        DropdownOptions = "[\"CallPerformance\", \"PhysicalAudit\", \"MysteryShopping\", \"OnlineEvaluation\", \"Survey\"]",
        SampleValue = "CallPerformance")]
    public ChecklistType ChecklistType { get; set; } = ChecklistType.CallPerformance;

    /// <summary>
    /// Puanlama yöntemi (Maksimum, Ortalama, Ağırlıklı Ortalama vb.)
    /// </summary>
    [ExcelColumn("Puanlama Yöntemi", 7, ColumnType = ExcelColumnType.Dropdown,
        Description = "Puanlama hesaplama yöntemi",
        DropdownOptions = "[\"Maximum\", \"Average\", \"WeightedAverage\", \"Sum\"]",
        SampleValue = "Maximum")]
    public ScoringMethod ScoringMethod { get; set; } = ScoringMethod.Maximum;

    /// <summary>
    /// Maksimum alınabilecek toplam puan
    /// </summary>
    [ExcelColumn("Maks Puan", 8, ColumnType = ExcelColumnType.Number,
        Description = "Kontrol listesinden alınabilecek maksimum puan", SampleValue = "100")]
    public decimal MaxTotalPoints { get; set; } = 100;

    /// <summary>
    /// Kontrol listesi kodu (benzersiz tanımlayıcı)
    /// </summary>
    [ExcelColumn("K.L. Kodu", 9, ColumnType = ExcelColumnType.Text,
        Description = "Kontrol listesi kodu", SampleValue = "KL-001")]
    public string? Code { get; set; }

    /// <summary>
    /// Şablon adı (kısa tanım)
    /// </summary>
    [ExcelColumn("Şablon", 10, ColumnType = ExcelColumnType.Text,
        Description = "Şablon adı", SampleValue = "Çağrı Performans")]
    public string? TemplateName { get; set; }

    /// <summary>
    /// Geçerlilik başlangıç tarihi
    /// </summary>
    [ExcelColumn("Geçerlilik Başlangıç", 11, ColumnType = ExcelColumnType.Date,
        Description = "Kontrol listesinin geçerli olduğu başlangıç tarihi")]
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Geçerlilik bitiş tarihi
    /// </summary>
    [ExcelColumn("Geçerlilik Bitiş", 12, ColumnType = ExcelColumnType.Date,
        Description = "Kontrol listesinin geçerli olduğu bitiş tarihi")]
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Tahmini tamamlama süresi (dakika)
    /// </summary>
    [ExcelColumn("Tahmini Süre (dk)", 13, ColumnType = ExcelColumnType.Number,
        Description = "Tahmini tamamlama süresi (dakika)", SampleValue = "30")]
    public int? EstimatedDurationMinutes { get; set; }

    // Navigation properties
    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
