namespace SecretCustomer.Core.Entities;

/// <summary>
/// Gölge Müşteri - Dönem Kupon (dönem bazlı kupon kodları)
/// </summary>
public class GmDonemKupon : BaseEntity
{
    /// <summary>
    /// Hangi döneme ait
    /// </summary>
    public int GmDonemId { get; set; }
    public GmDonem? GmDonem { get; set; }

    /// <summary>
    /// Kupon kodu
    /// </summary>
    public string KuponKodu { get; set; } = string.Empty;

    /// <summary>
    /// Kullanıldı mı?
    /// </summary>
    public bool IsUsed { get; set; } = false;
}
