namespace SecretCustomer.Core.Entities;

/// <summary>
/// Gölge Müşteri - Soru (hedef firmaya sorulacak soru)
/// </summary>
public class GmSoru : BaseEntity
{
    /// <summary>
    /// Hangi müşteriye ait
    /// </summary>
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>
    /// Hangi hedef firmaya ait
    /// </summary>
    public int GmHedefFirmaId { get; set; }
    public GmHedefFirma? GmHedefFirma { get; set; }

    /// <summary>
    /// Soru metni
    /// </summary>
    public string SoruMetni { get; set; } = string.Empty;

    /// <summary>
    /// Beklenen cevap (referans bilgi)
    /// </summary>
    public string? BeklenenCevap { get; set; }

    /// <summary>
    /// Varsayılan aranma sayısı
    /// </summary>
    public int AranmaSayisi { get; set; } = 1;

    /// <summary>
    /// Kuponlu soru mu?
    /// </summary>
    public bool IsKuponlu { get; set; } = false;

    /// <summary>
    /// Sıra numarası
    /// </summary>
    public int SiraNo { get; set; } = 0;

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;
}
