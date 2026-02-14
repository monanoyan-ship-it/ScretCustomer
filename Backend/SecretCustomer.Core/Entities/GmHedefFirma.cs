namespace SecretCustomer.Core.Entities;

/// <summary>
/// Gölge Müşteri - Hedef Firma (aranacak dış kurum)
/// </summary>
public class GmHedefFirma : BaseEntity
{
    /// <summary>
    /// Hangi müşteriye ait
    /// </summary>
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>
    /// Firma adı
    /// </summary>
    public string FirmaAdi { get; set; } = string.Empty;

    /// <summary>
    /// Telefon numarası
    /// </summary>
    public string? TelefonNo { get; set; }

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Aciklama { get; set; }

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<GmDonemSoru> DonemSorular { get; set; } = new List<GmDonemSoru>();
}
