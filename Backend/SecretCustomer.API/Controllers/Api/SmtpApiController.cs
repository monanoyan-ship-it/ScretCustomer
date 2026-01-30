using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Data;
using SecretCustomer.Services.Services;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/smtp")]
[ApiController]
[Authorize(Roles = "Admin")]
public class SmtpApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly SmtpEmailService _emailService;

    public SmtpApiController(ApplicationDbContext context, SmtpEmailService emailService)
    {
        _context = context;
        _emailService = emailService;
    }

    /// <summary>
    /// Tüm SMTP profillerini listele
    /// </summary>
    [HttpGet("profiles")]
    public async Task<IActionResult> GetAll()
    {
        var profiles = await _context.SmtpProfiles
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .Select(p => new SmtpProfileDto
            {
                Id = p.Id,
                Name = p.Name,
                Host = p.Host,
                Port = p.Port,
                Username = p.Username,
                Password = p.Password,
                UseSsl = p.UseSsl,
                FromEmail = p.FromEmail,
                FromName = p.FromName,
                Enabled = p.Enabled,
                IsDefault = p.IsDefault,
                UseOAuth = p.UseOAuth,
                TenantId = p.TenantId,
                ClientId = p.ClientId,
                ClientSecret = p.ClientSecret,
                UseGraphApi = p.UseGraphApi,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return Ok(profiles);
    }

    /// <summary>
    /// Tek profil getir
    /// </summary>
    [HttpGet("profiles/{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var profile = await _context.SmtpProfiles.FindAsync(id);
        if (profile == null)
            return NotFound(new { success = false, message = "Profil bulunamadı." });

        return Ok(new SmtpProfileDto
        {
            Id = profile.Id,
            Name = profile.Name,
            Host = profile.Host,
            Port = profile.Port,
            Username = profile.Username,
            Password = profile.Password,
            UseSsl = profile.UseSsl,
            FromEmail = profile.FromEmail,
            FromName = profile.FromName,
            Enabled = profile.Enabled,
            IsDefault = profile.IsDefault,
            UseOAuth = profile.UseOAuth,
            TenantId = profile.TenantId,
            ClientId = profile.ClientId,
            ClientSecret = profile.ClientSecret,
            UseGraphApi = profile.UseGraphApi,
            CreatedAt = profile.CreatedAt
        });
    }

    /// <summary>
    /// Yeni profil oluştur
    /// </summary>
    [HttpPost("profiles")]
    public async Task<IActionResult> Create([FromBody] SmtpProfileDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { success = false, message = "Profil adı gerekli." });

        if (string.IsNullOrWhiteSpace(dto.Host))
            return BadRequest(new { success = false, message = "SMTP sunucu adresi gerekli." });

        if (string.IsNullOrWhiteSpace(dto.FromEmail))
            return BadRequest(new { success = false, message = "Gönderen email adresi gerekli." });

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

        // Eğer bu profil default yapılıyorsa, diğerlerini false yap
        if (profile.IsDefault)
        {
            await ClearDefaultFlagAsync();
        }

        // Eğer hiç profil yoksa, ilk profili otomatik default yap
        var hasAnyProfile = await _context.SmtpProfiles.AnyAsync();
        if (!hasAnyProfile)
        {
            profile.IsDefault = true;
        }

        _context.SmtpProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Profil oluşturuldu.", id = profile.Id });
    }

    /// <summary>
    /// Profil güncelle
    /// </summary>
    [HttpPut("profiles/{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] SmtpProfileDto dto)
    {
        var profile = await _context.SmtpProfiles.FindAsync(id);
        if (profile == null)
            return NotFound(new { success = false, message = "Profil bulunamadı." });

        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { success = false, message = "Profil adı gerekli." });

        if (string.IsNullOrWhiteSpace(dto.Host))
            return BadRequest(new { success = false, message = "SMTP sunucu adresi gerekli." });

        if (string.IsNullOrWhiteSpace(dto.FromEmail))
            return BadRequest(new { success = false, message = "Gönderen email adresi gerekli." });

        profile.Name = dto.Name.Trim();
        profile.Host = dto.Host.Trim();
        profile.Port = dto.Port;
        profile.Username = dto.Username?.Trim();
        profile.Password = dto.Password;
        profile.UseSsl = dto.UseSsl;
        profile.FromEmail = dto.FromEmail.Trim();
        profile.FromName = dto.FromName?.Trim();
        profile.Enabled = dto.Enabled;
        profile.UseOAuth = dto.UseOAuth;
        profile.TenantId = dto.TenantId?.Trim();
        profile.ClientId = dto.ClientId?.Trim();
        profile.ClientSecret = dto.ClientSecret;
        profile.UseGraphApi = dto.UseGraphApi;

        // IsDefault güncelleme
        if (dto.IsDefault && !profile.IsDefault)
        {
            await ClearDefaultFlagAsync();
            profile.IsDefault = true;
        }

        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Profil güncellendi." });
    }

    /// <summary>
    /// Profil sil (soft delete)
    /// </summary>
    [HttpDelete("profiles/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var profile = await _context.SmtpProfiles.FindAsync(id);
        if (profile == null)
            return NotFound(new { success = false, message = "Profil bulunamadı." });

        if (profile.IsDefault)
            return BadRequest(new { success = false, message = "Varsayılan profil silinemez. Önce başka bir profili varsayılan yapın." });

        profile.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Profil silindi." });
    }

    /// <summary>
    /// Profili varsayılan yap
    /// </summary>
    [HttpPost("profiles/{id:int}/set-default")]
    public async Task<IActionResult> SetDefault(int id)
    {
        var profile = await _context.SmtpProfiles.FindAsync(id);
        if (profile == null)
            return NotFound(new { success = false, message = "Profil bulunamadı." });

        await ClearDefaultFlagAsync();
        profile.IsDefault = true;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = $"'{profile.Name}' varsayılan profil yapıldı." });
    }

    /// <summary>
    /// Belirli profilin SMTP bağlantısını test et
    /// </summary>
    [HttpPost("profiles/{id:int}/test-connection")]
    public async Task<IActionResult> TestConnection(int id)
    {
        var profile = await _context.SmtpProfiles.FindAsync(id);
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

        var profile = await _context.SmtpProfiles.FindAsync(id);
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

    /// <summary>
    /// Tüm profillerin IsDefault'ını false yapar
    /// </summary>
    private async Task ClearDefaultFlagAsync()
    {
        var defaultProfiles = await _context.SmtpProfiles
            .Where(p => p.IsDefault)
            .ToListAsync();

        foreach (var p in defaultProfiles)
        {
            p.IsDefault = false;
        }
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
                Bu bir test emailidir. Gönderim zamanı: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + @"
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
