using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Services.Services;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/smtp")]
[ApiController]
[Authorize(Roles = "Admin")]
public class SmtpApiController : ControllerBase
{
    private readonly ISmtpProfileService _smtpProfileService;
    private readonly SmtpEmailService _emailService;

    public SmtpApiController(ISmtpProfileService smtpProfileService, SmtpEmailService emailService)
    {
        _smtpProfileService = smtpProfileService;
        _emailService = emailService;
    }

    /// <summary>
    /// Tüm SMTP profillerini listele
    /// </summary>
    [HttpGet("profiles")]
    public async Task<IActionResult> GetAll()
    {
        var profiles = await _smtpProfileService.GetAllAsync();
        return Ok(profiles);
    }

    /// <summary>
    /// Tek profil getir
    /// </summary>
    [HttpGet("profiles/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var profile = await _smtpProfileService.GetByIdAsync(id);
        if (profile == null)
            return NotFound(new { success = false, message = "Profil bulunamadı." });

        return Ok(profile);
    }

    /// <summary>
    /// Yeni profil oluştur
    /// </summary>
    [HttpPost("profiles")]
    public async Task<IActionResult> Create([FromBody] SmtpProfileDto dto)
    {
        var profile = new SmtpProfile
        {
            Name = dto.Name.Trim(),
            Host = dto.Host.Trim(),
            Port = dto.Port,
            Username = dto.Username?.Trim(),
            Password = dto.Password,
            UseSsl = dto.UseSsl,
            FromEmail = dto.FromEmail.Trim(),
            FromName = dto.FromName?.Trim(),
            Enabled = dto.Enabled,
            IsDefault = dto.IsDefault,
            UseOAuth = dto.UseOAuth,
            TenantId = dto.TenantId?.Trim(),
            ClientId = dto.ClientId?.Trim(),
            ClientSecret = dto.ClientSecret,
            UseGraphApi = dto.UseGraphApi
        };

        var result = await _smtpProfileService.CreateAsync(profile, dto.IsDefault);

        if (!result.Success)
            return BadRequest(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message, id = result.Id });
    }

    /// <summary>
    /// Profil güncelle
    /// </summary>
    [HttpPut("profiles/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SmtpProfileDto dto)
    {
        var updatedData = new SmtpProfile
        {
            Name = dto.Name,
            Host = dto.Host,
            Port = dto.Port,
            Username = dto.Username,
            Password = dto.Password,
            UseSsl = dto.UseSsl,
            FromEmail = dto.FromEmail,
            FromName = dto.FromName,
            Enabled = dto.Enabled,
            UseOAuth = dto.UseOAuth,
            TenantId = dto.TenantId,
            ClientId = dto.ClientId,
            ClientSecret = dto.ClientSecret,
            UseGraphApi = dto.UseGraphApi
        };

        var result = await _smtpProfileService.UpdateAsync(id, updatedData, dto.IsDefault);

        if (!result.Success)
        {
            if (result.Message == "Profil bulunamadı.")
                return NotFound(new { success = false, message = result.Message });
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }

    /// <summary>
    /// Profil sil (soft delete)
    /// </summary>
    [HttpDelete("profiles/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _smtpProfileService.DeleteAsync(id);

        if (!result.Success)
        {
            if (result.Message == "Profil bulunamadı.")
                return NotFound(new { success = false, message = result.Message });
            return BadRequest(new { success = false, message = result.Message });
        }

        return Ok(new { success = true, message = result.Message });
    }

    /// <summary>
    /// Profili varsayılan yap
    /// </summary>
    [HttpPost("profiles/{id:int}/set-default")]
    public async Task<IActionResult> SetDefault(int id)
    {
        var result = await _smtpProfileService.SetDefaultAsync(id);

        if (!result.Success)
            return NotFound(new { success = false, message = result.Message });

        return Ok(new { success = true, message = result.Message });
    }

    /// <summary>
    /// Belirli profilin SMTP bağlantısını test et
    /// </summary>
    [HttpPost("profiles/{id:int}/test-connection")]
    public async Task<IActionResult> TestConnection(int id)
    {
        var profile = await _smtpProfileService.FindByIdAsync(id);
        if (profile == null)
            return NotFound(new { success = false, message = "Profil bulunamadı." });

        var result = await _emailService.TestConnectionWithProfileAsync(profile);

        if (result.Success)
            return Ok(new { success = true, message = "SMTP bağlantısı başarılı!" });

        return BadRequest(new { success = false, message = result.ErrorMessage });
    }

    /// <summary>
    /// Belirli profilden test maili gönder
    /// </summary>
    [HttpPost("profiles/{id:int}/send-test")]
    public async Task<IActionResult> SendTestEmail(int id, [FromBody] TestEmailDto dto)
    {
        if (string.IsNullOrEmpty(dto.ToEmail))
            return BadRequest(new { success = false, message = "Email adresi gerekli." });

        var profile = await _smtpProfileService.FindByIdAsync(id);
        if (profile == null)
            return NotFound(new { success = false, message = "Profil bulunamadı." });

        var message = new Core.Interfaces.Services.EmailMessage
        {
            To = new List<string> { dto.ToEmail },
            Subject = "Secret Customer - Test Email",
            Body = GetTestEmailBody(),
            IsHtml = true
        };

        var result = await _emailService.SendEmailWithProfileAsync(profile, message);

        if (result.Success)
            return Ok(new { success = true, message = $"Test maili {dto.ToEmail} adresine gönderildi." });

        return BadRequest(new { success = false, message = result.ErrorMessage });
    }

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
                Bu bir test emailidir. Gönderim zamanı: " + TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + @"
            </p>
        </div>
    </div>
</body>
</html>";
    }
}

public class SmtpProfileDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public bool UseSsl { get; set; } = true;
    public string FromEmail { get; set; } = string.Empty;
    public string? FromName { get; set; } = "Secret Customer";
    public bool Enabled { get; set; } = true;
    public bool IsDefault { get; set; } = false;
    // OAuth 2.0 (Microsoft 365)
    public bool UseOAuth { get; set; } = false;
    public string? TenantId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    // Microsoft Graph API
    public bool UseGraphApi { get; set; } = false;
    public DateTime? CreatedAt { get; set; }
}

public class TestEmailDto
{
    public string? ToEmail { get; set; }
}
