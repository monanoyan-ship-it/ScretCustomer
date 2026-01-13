using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Bayi Talebi - FieldWorker'ların oluşturduğu bayi ekleme/güncelleme talepleri
/// </summary>
public class DealerRequest : BaseEntity
{
    /// <summary>
    /// Talebin ait olduğu müşteri
    /// </summary>
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    /// <summary>
    /// Talebi oluşturan FieldWorker
    /// </summary>
    public int RequestedByUserId { get; set; }
    public User RequestedByUser { get; set; } = null!;

    /// <summary>
    /// Talep tipi (Yeni Bayi, Bayi Güncelleme)
    /// </summary>
    public int RequestTypeId { get; set; } = RequestTypes.Ids.NewDealer;

    /// <summary>
    /// Talep durumu (Beklemede, Onaylandı, Reddedildi)
    /// </summary>
    public int StatusId { get; set; } = RequestStatuses.Ids.Pending;

    /// <summary>
    /// Güncelleme talebi için mevcut bayi ID
    /// </summary>
    public int? DealerId { get; set; }
    public Dealer? Dealer { get; set; }

    /// <summary>
    /// Talep edilen bilgiler (JSON formatında)
    /// Yeni bayi için: tüm bayi bilgileri
    /// Güncelleme için: değiştirilmek istenen alanlar
    /// </summary>
    public string RequestDataJson { get; set; } = "{}";

    /// <summary>
    /// Admin yanıtı/açıklaması (onay veya red sebebi)
    /// </summary>
    public string? AdminResponse { get; set; }

    /// <summary>
    /// Talebi işleyen admin
    /// </summary>
    public int? ProcessedByUserId { get; set; }
    public User? ProcessedByUser { get; set; }

    /// <summary>
    /// Talebin işlenme tarihi
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
}
