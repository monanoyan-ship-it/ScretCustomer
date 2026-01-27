using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Kontrol Listesi - Değerlendirme formlarının şablonunu temsil eder
/// </summary>
[ExcelTemplate("Kontrol Listesi", Description = "Kontrol listeleri için Excel import/export", IsAvailable = true)]
public class Checklist : BaseEntity
{
    [ExcelColumn("Kontrol Listesi Adı", 1, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Kontrol listesinin adı", SampleValue = "Müşteri Memnuniyeti Anketi")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Açıklama", 2, IsRequired = true, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Kontrol listesi hakkında açıklama", SampleValue = "Genel müşteri memnuniyeti değerlendirmesi")]
    public string Description { get; set; } = string.Empty;

    [ExcelColumn("Puanlı", 3, ColumnType = ExcelColumnTypes.Ids.Boolean,
        Description = "Puanlı mı puansız mı?", SampleValue = "true")]
    public bool IsScored { get; set; } = true;

    [ExcelColumn("Aktif", 4, ColumnType = ExcelColumnTypes.Ids.Boolean,
        Description = "Kontrol listesi aktif mi?", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    [ExcelColumn("Versiyon", 5, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Kontrol listesi versiyonu", SampleValue = "1")]
    public int Version { get; set; } = 1;

    // ===== YENİ ALANLAR - Kontrol Listesi Geliştirmeleri =====

    /// <summary>
    /// Kontrol listesi tipi (ChecklistTypes.Ids kullanılır)
    /// </summary>
    [ExcelColumn("K.L. Tipi", 6, ColumnType = ExcelColumnTypes.Ids.Dropdown,
        Description = "Kontrol listesi tipi",
        DropdownOptions = "[\"CallPerformance\", \"PhysicalAudit\", \"MysteryShopping\", \"OnlineEvaluation\", \"Survey\", \"BankMysteryShopping\"]",
        SampleValue = "CallPerformance")]
    public int ChecklistTypeId { get; set; } = ChecklistTypes.Ids.CallPerformance;

    /// <summary>
    /// Puanlama yöntemi (Maksimum veya Kriter Toplam)
    /// </summary>
    [ExcelColumn("Puanlama Yöntemi", 7, ColumnType = ExcelColumnTypes.Ids.Dropdown,
        Description = "Puanlama hesaplama yöntemi",
        DropdownOptions = "[\"Maximum\", \"CriteriaTotal\"]",
        SampleValue = "Maximum")]
    public int ScoringMethodId { get; set; } = ScoringMethods.Ids.Maximum;

    /// <summary>
    /// Maksimum alınabilecek toplam puan
    /// </summary>
    [ExcelColumn("Maks Puan", 8, ColumnType = ExcelColumnTypes.Ids.Number,
        Description = "Kontrol listesinden alınabilecek maksimum puan", SampleValue = "100")]
    public decimal MaxTotalPoints { get; set; } = 100;

    /// <summary>
    /// Kontrol listesi kodu (benzersiz tanımlayıcı)
    /// </summary>
    [ExcelColumn("K.L. Kodu", 9, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Kontrol listesi kodu", SampleValue = "KL-001")]
    public string? Code { get; set; }

    /// <summary>
    /// Şablon adı (kısa tanım)
    /// </summary>
    [ExcelColumn("Şablon", 10, ColumnType = ExcelColumnTypes.Ids.Text,
        Description = "Şablon adı", SampleValue = "Çağrı Performans")]
    public string? TemplateName { get; set; }

    /// <summary>
    /// Geçerlilik başlangıç tarihi
    /// </summary>
    [ExcelColumn("Geçerlilik Başlangıç", 11, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "Kontrol listesinin geçerli olduğu başlangıç tarihi")]
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Geçerlilik bitiş tarihi
    /// </summary>
    [ExcelColumn("Geçerlilik Bitiş", 12, ColumnType = ExcelColumnTypes.Ids.Date,
        Description = "Kontrol listesinin geçerli olduğu bitiş tarihi")]
    public DateTime? ValidUntil { get; set; }

    // ===== GÖRÜNÜM AYARLARI =====

    /// <summary>
    /// Soru gruplarını gizle (formda grup isimleri görünmez, raporlamada kullanılır)
    /// </summary>
    public bool HideGroupNames { get; set; } = false;

    // ===== FİRMA VE ORGANİZASYON =====

    /// <summary>
    /// Firma ID (null ise tüm firmalar için genel)
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// Organizasyon/Şube ID (null ise tüm organizasyonlar için)
    /// </summary>
    public int? CustomerOrganizationId { get; set; }

    // Navigation properties
    public Customer? Customer { get; set; }
    public CustomerOrganization? CustomerOrganization { get; set; }

    /// <summary>
    /// Sorular - Direkt checklist'e bağlı sorular
    /// </summary>
    public ICollection<Question> Questions { get; set; } = new List<Question>();

    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
