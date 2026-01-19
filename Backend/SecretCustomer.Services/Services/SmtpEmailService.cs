using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using MimeKit;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using Microsoft.EntityFrameworkCore;

namespace SecretCustomer.Services.Services;

/// <summary>
/// SMTP üzerinden email gönderme servisi (OAuth 2.0 destekli)
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SmtpEmailService> _logger;

    // SMTP ayar key'leri
    private const string KEY_HOST = "Smtp.Host";
    private const string KEY_PORT = "Smtp.Port";
    private const string KEY_USERNAME = "Smtp.Username";
    private const string KEY_PASSWORD = "Smtp.Password";
    private const string KEY_USE_SSL = "Smtp.UseSsl";
    private const string KEY_FROM_EMAIL = "Smtp.FromEmail";
    private const string KEY_FROM_NAME = "Smtp.FromName";
    private const string KEY_ENABLED = "Smtp.Enabled";

    // OAuth 2.0 ayar key'leri
    private const string KEY_USE_OAUTH = "Smtp.UseOAuth";
    private const string KEY_TENANT_ID = "Smtp.TenantId";
    private const string KEY_CLIENT_ID = "Smtp.ClientId";
    private const string KEY_CLIENT_SECRET = "Smtp.ClientSecret";

    public SmtpEmailService(ApplicationDbContext context, ILogger<SmtpEmailService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EmailResult> SendEmailAsync(string to, string subject, string body, bool isHtml = true)
    {
        return await SendEmailAsync(new EmailMessage
        {
            To = new List<string> { to },
            Subject = subject,
            Body = body,
            IsHtml = isHtml
        });
    }

    public async Task<EmailResult> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true)
    {
        return await SendEmailAsync(new EmailMessage
        {
            To = to.ToList(),
            Subject = subject,
            Body = body,
            IsHtml = isHtml
        });
    }

    public async Task<EmailResult> SendEmailWithAttachmentAsync(string to, string subject, string body, List<EmailAttachment> attachments, bool isHtml = true)
    {
        return await SendEmailAsync(new EmailMessage
        {
            To = new List<string> { to },
            Subject = subject,
            Body = body,
            IsHtml = isHtml,
            Attachments = attachments
        });
    }

    public async Task<EmailResult> SendEmailAsync(EmailMessage message)
    {
        try
        {
            var settings = await GetSmtpSettingsAsync();
            if (settings == null)
            {
                return EmailResult.Fail("SMTP ayarları yapılandırılmamış.");
            }

            if (!settings.Enabled)
            {
                _logger.LogWarning("SMTP devre dışı, mail gönderilmedi: {Subject}", message.Subject);
                return EmailResult.Fail("SMTP servisi devre dışı.");
            }

            var mimeMessage = new MimeMessage();

            // From
            mimeMessage.From.Add(new MailboxAddress(settings.FromName, settings.FromEmail));

            // To
            foreach (var to in message.To)
            {
                mimeMessage.To.Add(MailboxAddress.Parse(to));
            }

            // CC
            foreach (var cc in message.Cc)
            {
                mimeMessage.Cc.Add(MailboxAddress.Parse(cc));
            }

            // BCC
            foreach (var bcc in message.Bcc)
            {
                mimeMessage.Bcc.Add(MailboxAddress.Parse(bcc));
            }

            // Reply-To
            if (!string.IsNullOrEmpty(message.ReplyTo))
            {
                mimeMessage.ReplyTo.Add(MailboxAddress.Parse(message.ReplyTo));
            }

            // Subject
            mimeMessage.Subject = message.Subject;

            // Body
            var bodyBuilder = new BodyBuilder();
            if (message.IsHtml)
            {
                bodyBuilder.HtmlBody = message.Body;
            }
            else
            {
                bodyBuilder.TextBody = message.Body;
            }

            // Attachments
            foreach (var attachment in message.Attachments)
            {
                bodyBuilder.Attachments.Add(attachment.FileName, attachment.Content, ContentType.Parse(attachment.ContentType));
            }

            mimeMessage.Body = bodyBuilder.ToMessageBody();

            // Send
            using var client = new SmtpClient();

            var secureSocketOptions = settings.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(settings.Host, settings.Port, secureSocketOptions);

            // OAuth 2.0 veya Basic Auth ile kimlik doğrulama
            if (settings.UseOAuth && !string.IsNullOrEmpty(settings.ClientId))
            {
                var accessToken = await GetOAuthTokenAsync(settings);
                var oauth2 = new SaslMechanismOAuth2(settings.Username, accessToken);
                await client.AuthenticateAsync(oauth2);
                _logger.LogDebug("OAuth 2.0 ile kimlik doğrulama yapıldı");
            }
            else if (!string.IsNullOrEmpty(settings.Username))
            {
                await client.AuthenticateAsync(settings.Username, settings.Password);
            }

            var messageId = await client.SendAsync(mimeMessage);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Email gönderildi: {To}, Subject: {Subject}", string.Join(",", message.To), message.Subject);

            return EmailResult.Ok(messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Email gönderilirken hata oluştu: {To}", string.Join(",", message.To));
            return EmailResult.Fail($"Email gönderilemedi: {ex.Message}");
        }
    }

    public async Task<EmailResult> TestConnectionAsync()
    {
        try
        {
            var settings = await GetSmtpSettingsAsync();
            if (settings == null)
            {
                return EmailResult.Fail("SMTP ayarları yapılandırılmamış.");
            }

            using var client = new SmtpClient();

            var secureSocketOptions = settings.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(settings.Host, settings.Port, secureSocketOptions);

            // OAuth 2.0 veya Basic Auth ile kimlik doğrulama
            if (settings.UseOAuth && !string.IsNullOrEmpty(settings.ClientId))
            {
                var accessToken = await GetOAuthTokenAsync(settings);
                var oauth2 = new SaslMechanismOAuth2(settings.Username, accessToken);
                await client.AuthenticateAsync(oauth2);
                _logger.LogDebug("OAuth 2.0 ile bağlantı testi yapıldı");
            }
            else if (!string.IsNullOrEmpty(settings.Username))
            {
                await client.AuthenticateAsync(settings.Username, settings.Password);
            }

            await client.DisconnectAsync(true);

            var authMethod = settings.UseOAuth ? "OAuth 2.0" : "Basic Auth";
            _logger.LogInformation("SMTP bağlantı testi başarılı: {Host}:{Port} ({AuthMethod})", settings.Host, settings.Port, authMethod);

            return EmailResult.Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP bağlantı testi başarısız");
            return EmailResult.Fail($"Bağlantı hatası: {ex.Message}");
        }
    }

    public async Task<bool> IsConfiguredAsync()
    {
        var settings = await GetSmtpSettingsAsync();
        return settings != null && !string.IsNullOrEmpty(settings.Host);
    }

    private async Task<SmtpSettings?> GetSmtpSettingsAsync()
    {
        var keys = new[] {
            KEY_HOST, KEY_PORT, KEY_USERNAME, KEY_PASSWORD, KEY_USE_SSL,
            KEY_FROM_EMAIL, KEY_FROM_NAME, KEY_ENABLED,
            KEY_USE_OAUTH, KEY_TENANT_ID, KEY_CLIENT_ID, KEY_CLIENT_SECRET
        };
        var settingsDict = await _context.SystemSettings
            .Where(s => keys.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        if (!settingsDict.ContainsKey(KEY_HOST) || string.IsNullOrEmpty(settingsDict[KEY_HOST]))
        {
            return null;
        }

        return new SmtpSettings
        {
            Host = settingsDict.GetValueOrDefault(KEY_HOST, ""),
            Port = int.TryParse(settingsDict.GetValueOrDefault(KEY_PORT, "587"), out var port) ? port : 587,
            Username = settingsDict.GetValueOrDefault(KEY_USERNAME, ""),
            Password = settingsDict.GetValueOrDefault(KEY_PASSWORD, ""),
            UseSsl = settingsDict.GetValueOrDefault(KEY_USE_SSL, "true").Equals("true", StringComparison.OrdinalIgnoreCase),
            FromEmail = settingsDict.GetValueOrDefault(KEY_FROM_EMAIL, ""),
            FromName = settingsDict.GetValueOrDefault(KEY_FROM_NAME, "Secret Customer"),
            Enabled = settingsDict.GetValueOrDefault(KEY_ENABLED, "true").Equals("true", StringComparison.OrdinalIgnoreCase),
            // OAuth 2.0 ayarları
            UseOAuth = settingsDict.GetValueOrDefault(KEY_USE_OAUTH, "false").Equals("true", StringComparison.OrdinalIgnoreCase),
            TenantId = settingsDict.GetValueOrDefault(KEY_TENANT_ID, ""),
            ClientId = settingsDict.GetValueOrDefault(KEY_CLIENT_ID, ""),
            ClientSecret = settingsDict.GetValueOrDefault(KEY_CLIENT_SECRET, "")
        };
    }

    /// <summary>
    /// Microsoft Entra ID'den OAuth 2.0 access token alır
    /// </summary>
    private async Task<string> GetOAuthTokenAsync(SmtpSettings settings)
    {
        var app = ConfidentialClientApplicationBuilder
            .Create(settings.ClientId)
            .WithClientSecret(settings.ClientSecret)
            .WithAuthority($"https://login.microsoftonline.com/{settings.TenantId}")
            .Build();

        // Microsoft 365 SMTP için gerekli scope
        var scopes = new[] { "https://outlook.office365.com/.default" };

        var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

        _logger.LogDebug("OAuth token alındı, geçerlilik: {ExpiresOn}", result.ExpiresOn);

        return result.AccessToken;
    }

    private class SmtpSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public bool UseSsl { get; set; } = true;
        public string FromEmail { get; set; } = "";
        public string FromName { get; set; } = "Secret Customer";
        public bool Enabled { get; set; } = true;

        // OAuth 2.0 ayarları
        public bool UseOAuth { get; set; } = false;
        public string TenantId { get; set; } = "";
        public string ClientId { get; set; } = "";
        public string ClientSecret { get; set; } = "";
    }
}
