using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Sistem dilleri
/// </summary>
public class Language
{
    public int Id { get; set; }

    /// <summary>
    /// Dil adı (Türkçe, English)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Dil kodu (tr-TR, en-US)
    /// </summary>
    public string LanguageCulture { get; set; } = string.Empty;

    /// <summary>
    /// Kısa kod (tr, en)
    /// </summary>
    public string UniqueSeoCode { get; set; } = string.Empty;

    /// <summary>
    /// Bayrak ikonu (flag-icon-tr, flag-icon-gb)
    /// </summary>
    public string? FlagImageFileName { get; set; }

    /// <summary>
    /// Sağdan sola yazım (Arapça vb.)
    /// </summary>
    public bool Rtl { get; set; } = false;

    /// <summary>
    /// Varsayılan dil mi?
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sıralama
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = TurkeyTime.Now;
    public DateTime? UpdatedAt { get; set; }

    // Navigation
    public virtual ICollection<LocaleStringResource> LocaleStringResources { get; set; } = new List<LocaleStringResource>();
}
