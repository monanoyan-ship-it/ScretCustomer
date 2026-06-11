using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Bir bayi birden fazla organizasyona bagli olabilir (many-to-many)
/// Atama bayi ekleme/guncelleme ekranindan yapilir
/// </summary>
public class CustomerDealerOrganization : BaseEntity
{
    public int CustomerDealerId { get; set; }
    public CustomerDealer CustomerDealer { get; set; } = null!;

    public int CustomerOrganizationId { get; set; }
    public CustomerOrganization CustomerOrganization { get; set; } = null!;

    /// <summary>
    /// Atama tarihi
    /// </summary>
    public DateTime AssignedAt { get; set; } = TurkeyTime.Now;

    /// <summary>
    /// Atama ile ilgili notlar
    /// </summary>
    public string? Notes { get; set; }
}
