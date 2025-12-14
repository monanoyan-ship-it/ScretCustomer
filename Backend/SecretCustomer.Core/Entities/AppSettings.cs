namespace SecretCustomer.Core.Entities;

/// <summary>
/// Dinamik ayar sistemi - Key-Value bazlı
/// </summary>
public class AppSettings
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Ayar anahtarı (örn: "General.DemoMode", "Security.MaxLoginAttempts")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Ayar değeri (string olarak saklanır, tip'e göre parse edilir)
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Veri tipi: String, Bool, Int, Decimal, Json, DateTime
    /// </summary>
    public SettingValueType ValueType { get; set; } = SettingValueType.String;

    /// <summary>
    /// Kategori (gruplandırma için)
    /// </summary>
    public string Category { get; set; } = "General";

    /// <summary>
    /// Açıklama (admin panelde gösterilir)
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Sıralama (admin panelde listeleme için)
    /// </summary>
    public int DisplayOrder { get; set; } = 0;

    /// <summary>
    /// Sistem ayarı mı? (true ise admin silemez, sadece değer değiştirebilir)
    /// </summary>
    public bool IsSystem { get; set; } = false;

    /// <summary>
    /// Nesne ID'si (opsiyonel - müşteriye/projeye özel ayarlar için)
    /// </summary>
    public Guid? EntityId { get; set; }

    /// <summary>
    /// Nesne tipi (opsiyonel - Customer, Project, Branch vb.)
    /// </summary>
    public string? EntityType { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public enum SettingValueType
{
    String = 0,
    Bool = 1,
    Int = 2,
    Decimal = 3,
    Json = 4,
    DateTime = 5
}
