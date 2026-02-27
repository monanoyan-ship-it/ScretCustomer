using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Gölge Müşteri - Atama (personele dağıtılmış tekil arama görevi)
/// </summary>
public class GmAtama : BaseEntity
{
    /// <summary>
    /// Hangi döneme ait
    /// </summary>
    public int GmDonemId { get; set; }
    public GmDonem? GmDonem { get; set; }

    /// <summary>
    /// Hangi dönem sorusu
    /// </summary>
    public int GmDonemSoruId { get; set; }
    public GmDonemSoru? GmDonemSoru { get; set; }

    /// <summary>
    /// Atanan kullanıcı
    /// </summary>
    public int UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// Planlanan tarih
    /// </summary>
    public DateTime? PlanTarihi { get; set; }

    /// <summary>
    /// Gerçekleşme tarihi
    /// </summary>
    public DateTime? GerceklesmeTarihi { get; set; }

    /// <summary>
    /// Arama saati
    /// </summary>
    public string? AramaSaati { get; set; }

    /// <summary>
    /// Not
    /// </summary>
    public string? Not { get; set; }

    /// <summary>
    /// Kupon kodu (kullanıcı tarafından girilir)
    /// </summary>
    public string? KuponKodu { get; set; }

    /// <summary>
    /// Kupon bilgisi (kuponlu sorular için - kullanıcının yazacağı bahis detayı)
    /// </summary>
    public string? KuponBilgisi { get; set; }

    /// <summary>
    /// Görüşülen temsilci ad soyad
    /// </summary>
    public string? GorusulenTemsilci { get; set; }

    /// <summary>
    /// Atama durumu (GmAtamaDurumlari)
    /// </summary>
    public int DurumId { get; set; } = GmAtamaDurumlari.Ids.Beklemede;
}
