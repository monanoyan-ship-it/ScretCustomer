using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Firma altındaki organizasyon yapısı
/// Hiyerarşi: Firma → Organizasyon → Personel → Bağlı Personel
/// </summary>
[ExcelTemplate("Müşteri Organizasyonu", Description = "Müşteri organizasyonları için Excel import/export", IsAvailable = true)]
public class CustomerOrganization : BaseEntity
{
    [ExcelColumn("Organizasyon Adı", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Organizasyonun adı", SampleValue = "Concentrix")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Organizasyon Kodu", 2, ColumnType = ExcelColumnType.Text,
        Description = "Organizasyon kodu", SampleValue = "CONC-001")]
    public string? Code { get; set; }

    [ExcelColumn("Açıklama", 3, ColumnType = ExcelColumnType.Text,
        Description = "Organizasyon açıklaması")]
    public string? Description { get; set; }

    [ExcelColumn("Seviye", 4, ColumnType = ExcelColumnType.Number,
        Description = "Hiyerarşi seviyesi (0=kök)", SampleValue = "0")]
    public int Level { get; set; } = 0;

    [ExcelColumn("Sıra", 5, ColumnType = ExcelColumnType.Number,
        Description = "Görüntüleme sırası", SampleValue = "1")]
    public int Order { get; set; } = 0;

    [ExcelColumn("Aktif", 6, ColumnType = ExcelColumnType.Boolean,
        Description = "Organizasyon aktif mi?", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    // Firma bağlantısı
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    // Hiyerarşi - Üst organizasyon
    public int? ParentId { get; set; }
    public CustomerOrganization? Parent { get; set; }

    // Hiyerarşi - Alt organizasyonlar
    public ICollection<CustomerOrganization> Children { get; set; } = new List<CustomerOrganization>();

    // Navigation - Bu organizasyondaki personeller
    public ICollection<CustomerPersonnel> Personnel { get; set; } = new List<CustomerPersonnel>();

    // Navigation - Bu organizasyonun yöneticileri (many-to-many)
    public ICollection<CustomerOrganizationManager> Managers { get; set; } = new List<CustomerOrganizationManager>();

    // Navigation - Bu organizasyonu değerlendirebilecekler (many-to-many)
    public ICollection<CustomerPersonnelOrganizationAccess> EvaluatorAccess { get; set; } = new List<CustomerPersonnelOrganizationAccess>();

    // Navigation - Bu organizasyonda gorevli personeller (many-to-many, her atamada farkli supervisor)
    public ICollection<CustomerPersonnelOrganization> PersonnelAssignments { get; set; } = new List<CustomerPersonnelOrganization>();
}
