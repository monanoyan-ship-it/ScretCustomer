namespace SecretCustomer.Core.Entities;

/// <summary>
/// Değerlendirici personelin hangi organizasyonları değerlendirebileceği (many-to-many)
/// Bir değerlendirici birden çok organizasyonu değerlendirebilir
/// </summary>
public class CustomerPersonnelOrganizationAccess : BaseEntity
{
    public int CustomerPersonnelId { get; set; }
    public CustomerPersonnel CustomerPersonnel { get; set; } = null!;

    public int CustomerOrganizationId { get; set; }
    public CustomerOrganization CustomerOrganization { get; set; } = null!;

    /// <summary>
    /// Bu organizasyonu değerlendirebilir mi?
    /// </summary>
    public bool CanEvaluate { get; set; } = true;

    /// <summary>
    /// Erişim atama tarihi
    /// </summary>
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
