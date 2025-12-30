namespace SecretCustomer.Core.Entities;

/// <summary>
/// Bir personel birden çok organizasyonu yönetebilir (many-to-many)
/// </summary>
public class CustomerOrganizationManager : BaseEntity
{
    public int CustomerPersonnelId { get; set; }
    public CustomerPersonnel CustomerPersonnel { get; set; } = null!;

    public int CustomerOrganizationId { get; set; }
    public CustomerOrganization CustomerOrganization { get; set; } = null!;

    /// <summary>
    /// Bu personelin ana organizasyonu mu?
    /// </summary>
    public bool IsPrimary { get; set; } = false;

    /// <summary>
    /// Yöneticilik atama tarihi
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
