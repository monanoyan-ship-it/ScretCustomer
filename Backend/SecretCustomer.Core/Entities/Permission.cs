using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Sistem yetkileri (Permissions)
/// </summary>
public class Permission : BaseEntity
{
    /// <summary>
    /// Yetki kodu (örn: "Projects.View", "Assignments.Create")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Görünen ad (örn: "Projeleri Görüntüle")
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Kategori
    /// </summary>
    public PermissionCategory Category { get; set; }

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Yetki aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sıralama numarası (UI'da gösterim sırası için)
    /// </summary>
    public int SortOrder { get; set; }

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<UserPermission> UserPermissions { get; set; } = new List<UserPermission>();
}
