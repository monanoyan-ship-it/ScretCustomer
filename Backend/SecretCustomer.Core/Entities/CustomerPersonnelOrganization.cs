using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Bir personel birden fazla organizasyonda gorev alabilir (many-to-many)
/// Her atamada farkli bir supervizorun altinda olabilir
/// </summary>
public class CustomerPersonnelOrganization : BaseEntity
{
    public int CustomerPersonnelId { get; set; }
    public CustomerPersonnel CustomerPersonnel { get; set; } = null!;

    public int CustomerOrganizationId { get; set; }
    public CustomerOrganization CustomerOrganization { get; set; } = null!;

    /// <summary>
    /// Bu organizasyondaki supervizor (her organizasyon icin farkli olabilir)
    /// </summary>
    public int? SupervisorId { get; set; }
    public CustomerPersonnel? Supervisor { get; set; }

    /// <summary>
    /// Atama tarihi
    /// </summary>
    public DateTime AssignedAt { get; set; } = TurkeyTime.Now;

    /// <summary>
    /// Atama ile ilgili notlar
    /// </summary>
    public string? Notes { get; set; }
}
