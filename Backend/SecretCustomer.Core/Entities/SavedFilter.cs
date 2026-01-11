namespace SecretCustomer.Core.Entities;

/// <summary>
/// Kullanıcının kaydettiği filtre sorguları
/// </summary>
public class SavedFilter : BaseEntity
{
    /// <summary>
    /// Filtreyi kaydeden kullanıcı
    /// </summary>
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// Filtrenin kullanıldığı sayfa (örn: "Listenings", "Assignments")
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
