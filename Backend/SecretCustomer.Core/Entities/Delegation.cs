using SecretCustomer.Core.Attributes;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Vekalet - Bir kullanıcının başka bir kullanıcı/birim adına yetki kullanması
/// </summary>
[ExcelTemplate("Vekalet", Description = "Vekalet tanımları için Excel import/export", IsAvailable = true)]
public class Delegation : BaseEntity
{
    [ExcelColumn("Vekalet Adı", 1, ColumnType = ExcelColumnType.Text,
        Description = "Vekalet tanımı", SampleValue = "Yıllık İzin Vekaleti")]
    public string? Title { get; set; }

    [ExcelColumn("Açıklama", 2, ColumnType = ExcelColumnType.Text,
        Description = "Vekalet açıklaması")]
    public string? Description { get; set; }

    // Vekalet veren (Delegator)
    public Guid DelegatorUserId { get; set; }
    public User DelegatorUser { get; set; } = null!;

    // Vekalet veren birim (opsiyonel)
    public Guid? DelegatorOrganizationUnitId { get; set; }
    public OrganizationUnit? DelegatorOrganizationUnit { get; set; }

    // Vekalet alan (Delegatee)
    public Guid DelegateeUserId { get; set; }
    public User DelegateeUser { get; set; } = null!;

    // Vekalet alan birim (opsiyonel)
    public Guid? DelegateeOrganizationUnitId { get; set; }
    public OrganizationUnit? DelegateeOrganizationUnit { get; set; }

    [ExcelColumn("Başlangıç Tarihi", 3, IsRequired = true, ColumnType = ExcelColumnType.Date,
        Description = "Vekalet başlangıç tarihi", SampleValue = "2024-01-01")]
    public DateTime StartDate { get; set; }

    [ExcelColumn("Bitiş Tarihi", 4, ColumnType = ExcelColumnType.Date,
        Description = "Vekalet bitiş tarihi", SampleValue = "2024-01-15")]
    public DateTime? EndDate { get; set; }

    [ExcelColumn("Aktif", 5, ColumnType = ExcelColumnType.Boolean,
        Description = "Vekalet aktif mi?", SampleValue = "true")]
    public bool IsActive { get; set; } = true;

    [ExcelColumn("Vekalet Tipi", 6, ColumnType = ExcelColumnType.Dropdown,
        Description = "Vekalet tipi",
        DropdownOptions = "[\"Full\", \"ReadOnly\", \"ApprovalOnly\"]",
        SampleValue = "Full")]
    public DelegationType DelegationType { get; set; } = DelegationType.Full;

    // Vekalet verilen yetkiler (JSON olarak saklanabilir)
    public string? PermissionScope { get; set; }

    // Vekalet sebebi
    [ExcelColumn("Sebep", 7, ColumnType = ExcelColumnType.Dropdown,
        Description = "Vekalet sebebi",
        DropdownOptions = "[\"AnnualLeave\", \"SickLeave\", \"BusinessTrip\", \"Training\", \"Other\"]",
        SampleValue = "AnnualLeave")]
    public DelegationReason Reason { get; set; } = DelegationReason.Other;

    [ExcelColumn("Notlar", 8, ColumnType = ExcelColumnType.Text,
        Description = "Ek notlar")]
    public string? Notes { get; set; }

    // Onay durumu
    public bool IsApproved { get; set; } = false;
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public User? ApprovedByUser { get; set; }
}
