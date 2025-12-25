using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Organizasyon Birimi - Hiyerarşik yapı için
/// </summary>
[ExcelTemplate("Organizasyon Birimi", Description = "Organizasyon birimleri için Excel import/export", IsAvailable = true)]
public class OrganizationUnit : BaseEntity
{
    [ExcelColumn("Birim Adı", 1, IsRequired = true, ColumnType = ExcelColumnType.Text,
        Description = "Organizasyon biriminin adı", SampleValue = "Satış Departmanı")]
    public string Name { get; set; } = string.Empty;

    [ExcelColumn("Birim Kodu", 2, ColumnType = ExcelColumnType.Text,
        Description = "Birim kodu", SampleValue = "SATIS-001")]
    public string? Code { get; set; }

    [ExcelColumn("Açıklama", 3, ColumnType = ExcelColumnType.Text,
        Description = "Birim açıklaması")]
    public string? Description { get; set; }

    [ExcelColumn("Birim Tipi", 4, ColumnType = ExcelColumnType.Dropdown,
        Description = "Birim tipi",
        DropdownOptions = "[\"Company\", \"Department\", \"Division\", \"Team\", \"Unit\"]",
        SampleValue = "Department")]
    public OrganizationUnitType UnitType { get; set; } = OrganizationUnitType.Department;

    [ExcelColumn("Seviye", 5, ColumnType = ExcelColumnType.Number,
        Description = "Hiyerarşi seviyesi (0=kök)", SampleValue = "1")]
    public int Level { get; set; } = 0;

    [ExcelColumn("Sıra", 6, ColumnType = ExcelColumnType.Number,
        Description = "Görüntüleme sırası", SampleValue = "1")]
    public int Order { get; set; } = 0;

    [ExcelColumn("Aktif", 7, ColumnType = ExcelColumnType.Boolean,
        Description = "Birim aktif mi?", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    // Hierarchy - Parent
    public int? ParentId { get; set; }
    public OrganizationUnit? Parent { get; set; }

    // Hierarchy - Children
    public ICollection<OrganizationUnit> Children { get; set; } = new List<OrganizationUnit>();

    // Manager
    public int? ManagerId { get; set; }
    public User? Manager { get; set; }

    // Location/Branch association
    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    // Navigation
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Delegation> DelegationsFrom { get; set; } = new List<Delegation>();
    public ICollection<Delegation> DelegationsTo { get; set; } = new List<Delegation>();
}
