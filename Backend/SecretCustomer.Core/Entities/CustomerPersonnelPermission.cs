using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Müşteri personellerine verilen özel izinler
/// </summary>
public class CustomerPersonnelPermission : BaseEntity
{
    public int PersonnelId { get; set; }
    public CustomerPersonnel Personnel { get; set; } = null!;

    /// <summary>
    /// İzin tipi
    /// </summary>
    public CustomerPermissionType PermissionType { get; set; }

    /// <summary>
    /// İzin kapsamı
    /// </summary>
    public PermissionScope Scope { get; set; } = PermissionScope.Customer;

    /// <summary>
    /// İzin aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// İznin geçerlilik başlangıç tarihi
    /// </summary>
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// İznin geçerlilik bitiş tarihi
    /// </summary>
    public DateTime? ValidUntil { get; set; }

    /// <summary>
    /// İzin açıklaması
    /// </summary>
    public string? Description { get; set; }
}
