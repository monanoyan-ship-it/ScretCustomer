using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Rol-Yetki ilişkisi (Many-to-Many)
/// </summary>
public class RolePermission : BaseEntity
{
    /// <summary>
    /// Rol
    /// </summary>
    public UserRole Role { get; set; }

    /// <summary>
    /// Yetki ID
    /// </summary>
    public Guid PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;

    /// <summary>
    /// Yetki verildi mi?
    /// </summary>
    public bool IsGranted { get; set; } = true;

    /// <summary>
    /// Yetki kapsamı (All, Own, Branch, vb.)
    /// </summary>
    public PermissionScope Scope { get; set; } = PermissionScope.All;

    /// <summary>
    /// Açıklama/Not
    /// </summary>
    public string? Notes { get; set; }
}
