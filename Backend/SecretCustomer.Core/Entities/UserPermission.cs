using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Kullanıcıya özel yetkiler (Rol yetkilerini override eder)
/// </summary>
public class UserPermission : BaseEntity
{
    /// <summary>
    /// Kullanıcı ID
    /// </summary>
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Yetki ID
    /// </summary>
    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;

    /// <summary>
    /// Yetki verildi mi? (false ise roldeki yetki iptal edilir)
    /// </summary>
    public bool IsGranted { get; set; } = true;

    /// <summary>
    /// Yetki kapsamı
    /// </summary>
    public PermissionScope Scope { get; set; } = PermissionScope.All;

    /// <summary>
    /// Geçerlilik başlangıç tarihi
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Geçerlilik bitiş tarihi
    /// </summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// Açıklama/Not
    /// </summary>
    public string? Notes { get; set; }
}
