namespace SecretCustomer.Core.Entities;

/// <summary>
/// Sistem ayarları (key-value)
/// </summary>
public class SystemSetting : BaseEntity
{
    /// <summary>
    /// Ayar anahtarı (unique)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Ayar değeri
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Ayar açıklaması
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Değer tipi (string, int, decimal, bool)
    /// </summary>
    public string ValueType { get; set; } = "string";

    /// <summary>
    /// Ayar kategorisi
    /// </summary>
    public string? Category { get; set; }
}

/// <summary>
/// Bilinen ayar anahtarları
/// </summary>
public static class SystemSettingKeys
{
    /// <summary>
    /// Günlük değerlendirme hedefi
    /// </summary>
    public const string DailyEvaluationTarget = "DailyEvaluationTarget";

    /// <summary>
    /// Varsayılan dönem hedefi
    /// </summary>
    public const string DefaultPeriodTarget = "DefaultPeriodTarget";

    // SMTP Ayarları
    public const string SmtpHost = "Smtp.Host";
    public const string SmtpPort = "Smtp.Port";
    public const string SmtpUsername = "Smtp.Username";
    public const string SmtpPassword = "Smtp.Password";
    public const string SmtpUseSsl = "Smtp.UseSsl";
    public const string SmtpFromEmail = "Smtp.FromEmail";
    public const string SmtpFromName = "Smtp.FromName";
    public const string SmtpEnabled = "Smtp.Enabled";
}
