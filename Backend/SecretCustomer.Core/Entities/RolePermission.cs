using SecretCustomer.Core.Enums;


namespace SecretCustomer.Core.Entities;

/// <summary>
/// Rol-Yetki ilişkisi (Many-to-Many)
/// </summary>
public class RolePermission : BaseEntity
{
    /// <summary>
    /// Rol ID (UserRoles.Ids kullanılır)
    /// </summary>
    public int RoleId { get; set; }

    /// <summary>
    /// Yetki ID
    /// </summary>
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;

    /// <summary>
    /// Yetki verildi mi?
    /// </summary>
    public bool IsGranted { get; set; } = true;

    /// <summary>
    /// Yetki kapsamı (All, Own, Branch, vb.)
    /// </summary>
    public int ScopeId { get; set; } = PermissionScopes.Ids.All;

    /// <summary>
    /// Açıklama/Not
    /// </summary>
    public string? Notes { get; set; }
}
