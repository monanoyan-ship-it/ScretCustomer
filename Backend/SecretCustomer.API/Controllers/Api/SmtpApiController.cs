using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/smtp")]
[ApiController]
[Authorize(Roles = "Admin")]
public class SmtpApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly IEmailService _emailService;

    public SmtpApiController(ApplicationDbContext context, IEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    /// <summary>
    /// SMTP ayarlarını getir
    /// </summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetSettings()
    {
        var keys = new[]
        {
            SystemSettingKeys.SmtpHost,
            SystemSettingKeys.SmtpPort,
            SystemSettingKeys.SmtpUsername,
            SystemSettingKeys.SmtpPassword,
            SystemSettingKeys.SmtpUseSsl,
            SystemSettingKeys.SmtpFromEmail,
            SystemSettingKeys.SmtpFromName,
            SystemSettingKeys.SmtpEnabled,
            // OAuth 2.0
            SystemSettingKeys.SmtpUseOAuth,
            SystemSettingKeys.SmtpTenantId,
            SystemSettingKeys.SmtpClientId,
            SystemSettingKeys.SmtpClientSecret
        };

        var settings = await _context.SystemSettings
            .Where(s => keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        return Ok(new SmtpSettingsDto
        {
            Host = settings.GetValueOrDefault(SystemSettingKeys.SmtpHost, ""),
            Port = int.TryParse(settings.GetValueOrDefault(SystemSettingKeys.SmtpPort, "587"), out var port) ? port : 587,
            Username = settings.GetValueOrDefault(SystemSettingKeys.SmtpUsername, ""),
            Password = settings.GetValueOrDefault(SystemSettingKeys.SmtpPassword, ""),
            UseSsl = settings.GetValueOrDefault(SystemSettingKeys.SmtpUseSsl, "true").Equals("true", StringComparison.OrdinalIgnoreCase),
            FromEmail = settings.GetValueOrDefault(SystemSettingKeys.SmtpFromEmail, ""),
            FromName = settings.GetValueOrDefault(SystemSettingKeys.SmtpFromName, "Secret Customer"),
            Enabled = settings.GetValueOrDefault(SystemSettingKeys.SmtpEnabled, "true").Equals("true", StringComparison.OrdinalIgnoreCase),
            // OAuth 2.0
            UseOAuth = settings.GetValueOrDefault(SystemSettingKeys.SmtpUseOAuth, "false").Equals("true", StringComparison.OrdinalIgnoreCase),
            TenantId = settings.GetValueOrDefault(SystemSettingKeys.SmtpTenantId, ""),
            ClientId = settings.GetValueOrDefault(SystemSettingKeys.SmtpClientId, ""),
            ClientSecret = settings.GetValueOrDefault(SystemSettingKeys.SmtpClientSecret, "")
        });
    }

    /// <summary>
    /// SMTP ayarlarını kaydet
    /// </summary>
    [HttpPost("settings")]
    public async Task<IActionResult> SaveSettings([FromBody] SmtpSettingsDto dto)
    {
        var settingsToSave = new Dictionary<string, string>
        {
            { SystemSettingKeys.SmtpHost, dto.Host ?? "" },
            { SystemSettingKeys.SmtpPort, dto.Port.ToString() },
            { SystemSettingKeys.SmtpUsername, dto.Username ?? "" },
            { SystemSettingKeys.SmtpPassword, dto.Password ?? "" },
            { SystemSettingKeys.SmtpUseSsl, dto.UseSsl.ToString().ToLower() },
            { SystemSettingKeys.SmtpFromEmail, dto.FromEmail ?? "" },
            { SystemSettingKeys.SmtpFromName, dto.FromName ?? "Secret Customer" },
            { SystemSettingKeys.SmtpEnabled, dto.Enabled.ToString().ToLower() },
            // OAuth 2.0
            { SystemSettingKeys.SmtpUseOAuth, dto.UseOAuth.ToString().ToLower() },
            { SystemSettingKeys.SmtpTenantId, dto.TenantId ?? "" },
            { SystemSettingKeys.SmtpClientId, dto.ClientId ?? "" },
            { SystemSettingKeys.SmtpClientSecret, dto.ClientSecret ?? "" }
        };

        foreach (var kvp in settingsToSave)
        {
            var existing = await _context.SystemSettings.FirstOrDefaultAsync(s => s.Key == kvp.Key);
            if (existing != null)
            {
                existing.Value = kvp.Value;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                _context.SystemSettings.Add(new SystemSetting
                {
                    Key = kvp.Key,
                    Value = kvp.Value,
                    Category = "Smtp",
                    ValueType = kvp.Key == SystemSettingKeys.SmtpPort ? "int" :
                               (kvp.Key == SystemSettingKeys.SmtpUseSsl || kvp.Key == SystemSettingKeys.SmtpEnabled ? "bool" : "string"),
                    Description = GetSettingDescription(kvp.Key),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "SMTP ayarları kaydedildi." });
    }

    /// <summary>
    /// SMTP bağlantısını test et
    /// </summary>
    [HttpPost("test-connection")]
    public async Task<IActionResult> TestConnection()
    {
        var result = await _emailService.TestConnectionAsync();

        if (result.Success)
        {
            return Ok(new { success = true, message = "SMTP bağlantısı başarılı!" });
        }

        return BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Test maili gönder
    /// </summary>
    [HttpPost("send-test")]
    public async Task<IActionResult> SendTestEmail([FromBody] TestEmailDto dto)
    {
        if (string.IsNullOrEmpty(dto.ToEmail))
        {
            return BadRequest(new { success = false, message = "Email adresi gerekli." });
        }

        var result = await _emailService.SendEmailAsync(
            dto.ToEmail,
            "Secret Customer - Test Email",
            GetTestEmailBody(),
            isHtml: true
        );

        if (result.Success)
        {
            return Ok(new { success = true, message = $"Test maili {dto.ToEmail} adresine gönderildi." });
        }

        return BadRequest(new { success = false, message = result.ErrorMessage });
    }

    private static string GetSettingDescription(string key) => key switch
    {
        SystemSettingKeys.SmtpHost => "SMTP sunucu adresi",
        SystemSettingKeys.SmtpPort => "SMTP port numarası",
        SystemSettingKeys.SmtpUsername => "SMTP kullanıcı adı",
        SystemSettingKeys.SmtpPassword => "SMTP şifresi",
        SystemSettingKeys.SmtpUseSsl => "SSL/TLS kullan",
        SystemSettingKeys.SmtpFromEmail => "Gönderen email adresi",
        SystemSettingKeys.SmtpFromName => "Gönderen adı",
        SystemSettingKeys.SmtpEnabled => "SMTP servisi aktif",
        // OAuth 2.0
        SystemSettingKeys.SmtpUseOAuth => "OAuth 2.0 kullan",
        SystemSettingKeys.SmtpTenantId => "Azure Tenant ID",
        SystemSettingKeys.SmtpClientId => "Azure Client ID (Application ID)",
        SystemSettingKeys.SmtpClientSecret => "Azure Client Secret",
        _ => ""
    };

    private static string GetTestEmailBody()
    {
        return @"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #0d6efd; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }
        .content { background: #f8f9fa; padding: 20px; border-radius: 0 0 8px 8px; }
        .success { color: #198754; font-weight: bold; }
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Secret Customer</h1>
        </div>
        <div class='content'>
            <h2 class='success'>SMTP Yapılandırması Başarılı!</h2>
            <p>Bu email, SMTP ayarlarınızın doğru yapılandırıldığını doğrulamak için gönderilmiştir.</p>
            <p>Artık sistemden email gönderebilirsiniz:</p>
            <ul>
                <li>Anket davetiyeleri</li>
                <li>Değerlendirme bildirimleri</li>
                <li>Şifre sıfırlama</li>
                <li>Raporlar</li>
            </ul>
            <hr>
            <p style='font-size: 12px; color: #666;'>
                Bu bir test emailidir. Gönderim zamanı: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + @"
            </p>
        </div>
    </div>
</body>
</html>";
    }
}

public class SmtpSettingsDto
{
    public string? Host { get; set; }
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; } = true;
    public string? FromEmail { get; set; }
    public string? FromName { get; set; } = "Secret Customer";
    public bool Enabled { get; set; } = true;

    // OAuth 2.0 (Microsoft 365)
    public bool UseOAuth { get; set; } = false;
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
}

public class TestEmailDto
{
    public string? ToEmail { get; set; }
}
