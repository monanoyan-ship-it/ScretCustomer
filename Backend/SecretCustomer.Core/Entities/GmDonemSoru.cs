namespace SecretCustomer.Core.Entities;

/// <summary>
/// Gölge Müşteri - Dönem Soru (döneme ait soru, tüm soru bilgilerini içerir)
/// </summary>
public class GmDonemSoru : BaseEntity
{
    /// <summary>
    /// Hangi döneme ait
    /// </summary>
    public int GmDonemId { get; set; }
    public GmDonem? GmDonem { get; set; }

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
    /// Aranma sayısı
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
    /// Kupon kodu (kuponlu sorular için)
    /// </summary>
    public string? KuponKodu { get; set; }

    /// <summary>
    /// Eski GmSoru FK (nullable, geriye dönük uyumluluk)
    /// </summary>
    public int? GmSoruId { get; set; }
    public GmSoru? GmSoru { get; set; }
}
