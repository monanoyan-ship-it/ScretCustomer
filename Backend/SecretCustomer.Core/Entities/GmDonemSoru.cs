namespace SecretCustomer.Core.Entities;

/// <summary>
/// Gölge Müşteri - Dönem Soru (döneme eklenmiş soru + dönem bazlı override)
/// </summary>
public class GmDonemSoru : BaseEntity
{
    /// <summary>
    /// Hangi döneme ait
    /// </summary>
    public int GmDonemId { get; set; }
    public GmDonem? GmDonem { get; set; }

    /// <summary>
    /// Hangi soru
    /// </summary>
    public int GmSoruId { get; set; }
    public GmSoru? GmSoru { get; set; }

    /// <summary>
    /// Dönem bazlı aranma sayısı override
    /// </summary>
    public int AranmaSayisi { get; set; } = 1;
}
