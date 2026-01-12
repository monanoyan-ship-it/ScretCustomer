namespace SecretCustomer.Core.Entities;

/// <summary>
/// Kayıtlı filtre sorguları - hem admin hem customer portal için
/// </summary>
public class SavedFilter : BaseEntity
{
    /// <summary>
    /// Filtreyi kaydeden kullanıcı (admin panel için, bilgi amaçlı)
    /// Null ise customer portal'dan kaydedilmiş
    /// </summary>
    public int? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>
    /// Customer ID - CustomerPortal filtreleri için
    /// Null ise admin panel filtresi (tüm adminler görür)
    /// Dolu ise sadece o customer'ın personelleri görür
    /// </summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>
    /// Filtrenin kullanıldığı sayfa (örn: "Listenings", "InternalEvaluations")
    /// </summary>
    public string PageName { get; set; } = string.Empty;

    /// <summary>
    /// Filtre adı/başlığı
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Filtre açıklaması (opsiyonel)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Filtre verileri (JSON formatında)
    /// </summary>
    public string FilterData { get; set; } = string.Empty;

    /// <summary>
    /// Varsayılan filtre mi?
    /// </summary>
    public bool IsDefault { get; set; } = false;
}
