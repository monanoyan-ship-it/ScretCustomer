using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Gölge Müşteri - Dönem (aylık plan)
/// </summary>
public class GmDonem : BaseEntity
{
    /// <summary>
    /// Hangi müşteriye ait
    /// </summary>
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>
    /// Dönem adı (örn: "Mart 2026")
    /// </summary>
    public string Ad { get; set; } = string.Empty;

    /// <summary>
    /// Dönem başlangıç tarihi
    /// </summary>
    public DateTime BaslangicTarihi { get; set; }

    /// <summary>
    /// Dönem bitiş tarihi
    /// </summary>
    public DateTime BitisTarihi { get; set; }

    /// <summary>
    /// Dönem durumu (GmDonemDurumlari)
    /// </summary>
    public int DurumId { get; set; } = GmDonemDurumlari.Ids.Taslak;

    /// <summary>
    /// Oluşturan kullanıcı
    /// </summary>
    public int? OlusturanUserId { get; set; }
    public User? OlusturanUser { get; set; }

    // Navigation
    public ICollection<GmDonemKupon> Kuponlar { get; set; } = new List<GmDonemKupon>();
    public ICollection<GmDonemPersonel> Personeller { get; set; } = new List<GmDonemPersonel>();
    public ICollection<GmDonemSoru> Sorular { get; set; } = new List<GmDonemSoru>();
    public ICollection<GmAtama> Atamalar { get; set; } = new List<GmAtama>();
}
