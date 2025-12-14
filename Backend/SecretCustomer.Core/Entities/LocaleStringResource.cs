namespace SecretCustomer.Core.Entities;

/// <summary>
/// Çeviri kaynakları - T("Key") ile erişilir
/// </summary>
public class LocaleStringResource
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Dil ID
    /// </summary>
    public Guid LanguageId { get; set; }

    /// <summary>
    /// Kaynak anahtarı (örn: "Login.Title", "Common.Save", "Menu.Dashboard")
    /// </summary>
    public string ResourceName { get; set; } = string.Empty;

    /// <summary>
    /// Çeviri değeri
    /// </summary>
    public string ResourceValue { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual Language Language { get; set; } = null!;
}
