namespace SecretCustomer.Core.Entities;

/// <summary>
/// SMTP profili - Birden fazla SMTP yapılandırması destekler
/// </summary>
public class SmtpProfile : BaseEntity
{
    /// <summary>
    /// Profil adı (ör: "Office 365", "Gmail")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// SMTP sunucu adresi
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP port numarası
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// SMTP kullanıcı adı / email
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// SMTP şifresi
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// SSL/TLS kullan (StartTLS)
    /// </summary>
    public bool UseSsl { get; set; } = true;

    /// <summary>
    /// Gönderen email adresi
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Gönderen adı
    /// </summary>
    public string? FromName { get; set; }

    /// <summary>
    /// Profil aktif mi
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Varsayılan profil mi (sistemde tek bir default olmalı)
    /// </summary>
    public bool IsDefault { get; set; } = false;

    // ===== OAuth 2.0 (Microsoft 365) =====

    /// <summary>
    /// OAuth 2.0 kimlik doğrulama kullan
    /// </summary>
    public bool UseOAuth { get; set; } = false;

    /// <summary>
    /// Azure Active Directory Tenant ID
    /// </summary>
    public string? TenantId { get; set; }

    /// <summary>
    /// Azure Client ID (Application ID)
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Azure Client Secret
    /// </summary>
    public string? ClientSecret { get; set; }

    /// <summary>
    /// Microsoft Graph API kullan (SMTP yerine)
    /// OAuth etkinken Graph API ile email gönderir
    /// </summary>
    public bool UseGraphApi { get; set; } = false;
}
