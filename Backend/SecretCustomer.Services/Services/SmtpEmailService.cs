using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Identity.Client;
using MimeKit;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SecretCustomer.Services.Services;

/// <summary>
/// SMTP üzerinden email gönderme servisi (OAuth 2.0 ve Microsoft Graph API destekli, çoklu profil)
/// </summary>
public class SmtpEmailService : IEmailService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly IHttpClientFactory _httpClientFactory;

    public SmtpEmailService(ApplicationDbContext context, IAuditLogService auditLogService, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _auditLogService = auditLogService;
        _httpClientFactory = httpClientFactory;
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
            var profile = await GetDefaultProfileAsync();
            if (profile == null)
            {
                return EmailResult.Fail("SMTP ayarları yapılandırılmamış.");
            }

            if (!profile.Enabled)
            {
                await _auditLogService.LogWarningAsync($"SMTP devre dışı, mail gönderilmedi: {message.Subject}", "Email");
                return EmailResult.Fail("SMTP servisi devre dışı.");
            }

            return await SendEmailWithProfileAsync(profile, message);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Email gönderilirken hata oluştu: {string.Join(",", message.To)}", "Email", ex);
            return EmailResult.Fail($"Email gönderilemedi: {ex.Message}");
        }
    }

    /// <summary>
    /// Belirli bir profil ile email gönderir (controller'dan profil-specific gönderim için)
    /// </summary>
    public async Task<EmailResult> SendEmailWithProfileAsync(SmtpProfile profile, EmailMessage message)
    {
        // Microsoft Graph API kullan
        if (profile.UseOAuth && profile.UseGraphApi && !string.IsNullOrEmpty(profile.ClientId))
        {
            return await SendEmailWithGraphApiAsync(profile, message);
        }

        // SMTP ile gönder
        return await SendEmailWithSmtpAsync(profile, message);
    }

    /// <summary>
    /// Microsoft Graph API ile email gönderir
    /// </summary>
    private async Task<EmailResult> SendEmailWithGraphApiAsync(SmtpProfile profile, EmailMessage message)
    {
        try
        {
            var accessToken = await GetGraphApiTokenAsync(profile);
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            // Graph API email payload
            var emailPayload = new
            {
                message = new
                {
                    subject = message.Subject,
                    body = new
                    {
                        contentType = message.IsHtml ? "HTML" : "Text",
                        content = message.Body
                    },
                    from = new
                    {
                        emailAddress = new
                        {
                            address = profile.FromEmail,
                            name = profile.FromName ?? "Secret Customer"
                        }
                    },
                    toRecipients = message.To.Select(to => new
                    {
                        emailAddress = new { address = to }
                    }).ToArray(),
                    ccRecipients = message.Cc.Select(cc => new
                    {
                        emailAddress = new { address = cc }
                    }).ToArray(),
                    bccRecipients = message.Bcc.Select(bcc => new
                    {
                        emailAddress = new { address = bcc }
                    }).ToArray(),
                    replyTo = string.IsNullOrEmpty(message.ReplyTo) ? null : new[]
                    {
                        new { emailAddress = new { address = message.ReplyTo } }
                    },
                    attachments = message.Attachments.Select(att => new
                    {
                        @odata_type = "#microsoft.graph.fileAttachment",
                        name = att.FileName,
                        contentType = att.ContentType,
                        contentBytes = Convert.ToBase64String(att.Content)
                    }).ToArray()
                },
                saveToSentItems = true
            };

            var json = JsonSerializer.Serialize(emailPayload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });

            // @odata.type düzeltmesi (JsonSerializer camelCase yapıyor)
            json = json.Replace("\"odataType\"", "\"@odata.type\"");

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Graph API endpoint - kullanıcı adına email gönder
            var graphUrl = $"https://graph.microsoft.com/v1.0/users/{profile.FromEmail}/sendMail";

            var response = await httpClient.PostAsync(graphUrl, content);

            if (response.IsSuccessStatusCode)
            {
                await _auditLogService.LogInfoAsync($"Email gönderildi (Graph API - {profile.Name}): {string.Join(",", message.To)}, Subject: {message.Subject}", "Email");
                return EmailResult.Ok();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            await _auditLogService.LogErrorAsync($"Graph API hatası ({profile.Name}): {response.StatusCode} - {errorContent}", "Email");
            return EmailResult.Fail($"Graph API hatası: {response.StatusCode} - {errorContent}");
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Graph API ile email gönderilirken hata oluştu ({profile.Name}): {string.Join(",", message.To)}", "Email", ex);
            return EmailResult.Fail($"Graph API hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// SMTP ile email gönderir
    /// </summary>
    private async Task<EmailResult> SendEmailWithSmtpAsync(SmtpProfile profile, EmailMessage message)
    {
        try
        {
            var mimeMessage = new MimeMessage();

            // From
            mimeMessage.From.Add(new MailboxAddress(profile.FromName ?? "Secret Customer", profile.FromEmail));

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

            var secureSocketOptions = profile.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(profile.Host, profile.Port, secureSocketOptions);

            // OAuth 2.0 veya Basic Auth ile kimlik doğrulama
            if (profile.UseOAuth && !string.IsNullOrEmpty(profile.ClientId))
            {
                var accessToken = await GetSmtpOAuthTokenAsync(profile);
                var oauth2 = new SaslMechanismOAuth2(profile.Username, accessToken);
                await client.AuthenticateAsync(oauth2);
            }
            else if (!string.IsNullOrEmpty(profile.Username))
            {
                await client.AuthenticateAsync(profile.Username, profile.Password);
            }

            var messageId = await client.SendAsync(mimeMessage);
            await client.DisconnectAsync(true);

            await _auditLogService.LogInfoAsync($"Email gönderildi (SMTP - {profile.Name}): {string.Join(",", message.To)}, Subject: {message.Subject}", "Email");

            return EmailResult.Ok(messageId);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"SMTP ile email gönderilirken hata oluştu ({profile.Name}): {string.Join(",", message.To)}", "Email", ex);
            return EmailResult.Fail($"Email gönderilemedi: {ex.Message}");
        }
    }

    public async Task<EmailResult> TestConnectionAsync()
    {
        var profile = await GetDefaultProfileAsync();
        if (profile == null)
        {
            return EmailResult.Fail("SMTP ayarları yapılandırılmamış.");
        }

        return await TestConnectionWithProfileAsync(profile);
    }

    /// <summary>
    /// Belirli bir profil ile bağlantı testi yapar
    /// </summary>
    public async Task<EmailResult> TestConnectionWithProfileAsync(SmtpProfile profile)
    {
        // Graph API testi
        if (profile.UseOAuth && profile.UseGraphApi && !string.IsNullOrEmpty(profile.ClientId))
        {
            return await TestGraphApiConnectionAsync(profile);
        }

        // SMTP testi
        return await TestSmtpConnectionAsync(profile);
    }

    /// <summary>
    /// Microsoft Graph API bağlantı testi
    /// </summary>
    private async Task<EmailResult> TestGraphApiConnectionAsync(SmtpProfile profile)
    {
        try
        {
            // Token alabiliyorsak bağlantı başarılı
            var accessToken = await GetGraphApiTokenAsync(profile);

            // Opsiyonel: Kullanıcı bilgisini kontrol et
            var httpClient = _httpClientFactory.CreateClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync($"https://graph.microsoft.com/v1.0/users/{profile.FromEmail}");

            if (response.IsSuccessStatusCode)
            {
                await _auditLogService.LogInfoAsync($"Graph API bağlantı testi başarılı ({profile.Name}): {profile.FromEmail}", "Email");
                return EmailResult.Ok();
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            await _auditLogService.LogWarningAsync($"Graph API kullanıcı doğrulama hatası ({profile.Name}): {errorContent}", "Email");

            // Token alındıysa bağlantı başarılı sayılabilir, kullanıcı erişimi ayrı bir konu
            return EmailResult.Ok();
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Graph API bağlantı testi başarısız ({profile.Name})", "Email", ex);
            return EmailResult.Fail($"Graph API bağlantı hatası: {ex.Message}");
        }
    }

    /// <summary>
    /// SMTP bağlantı testi
    /// </summary>
    private async Task<EmailResult> TestSmtpConnectionAsync(SmtpProfile profile)
    {
        try
        {
            using var client = new SmtpClient();

            var secureSocketOptions = profile.UseSsl
                ? SecureSocketOptions.StartTls
                : SecureSocketOptions.Auto;

            await client.ConnectAsync(profile.Host, profile.Port, secureSocketOptions);

            // OAuth 2.0 veya Basic Auth ile kimlik doğrulama
            if (profile.UseOAuth && !string.IsNullOrEmpty(profile.ClientId))
            {
                var accessToken = await GetSmtpOAuthTokenAsync(profile);
                var oauth2 = new SaslMechanismOAuth2(profile.Username, accessToken);
                await client.AuthenticateAsync(oauth2);
            }
            else if (!string.IsNullOrEmpty(profile.Username))
            {
                await client.AuthenticateAsync(profile.Username, profile.Password);
            }

            await client.DisconnectAsync(true);

            var authMethod = profile.UseOAuth ? "OAuth 2.0 SMTP" : "Basic Auth";
            await _auditLogService.LogInfoAsync($"SMTP bağlantı testi başarılı ({profile.Name}): {profile.Host}:{profile.Port} ({authMethod})", "Email");

            return EmailResult.Ok();
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"SMTP bağlantı testi başarısız ({profile.Name})", "Email", ex);
            return EmailResult.Fail($"Bağlantı hatası: {ex.Message}");
        }
    }

    public async Task<bool> IsConfiguredAsync()
    {
        var profile = await GetDefaultProfileAsync();
        if (profile == null) return false;

        // Graph API kullanılıyorsa ClientId yeterli
        if (profile.UseGraphApi)
        {
            return !string.IsNullOrEmpty(profile.ClientId);
        }

        // SMTP için Host gerekli
        return !string.IsNullOrEmpty(profile.Host);
    }

    /// <summary>
    /// Default SMTP profilini getirir
    /// </summary>
    private async Task<SmtpProfile?> GetDefaultProfileAsync()
    {
        return await _context.SmtpProfiles
            .Where(p => p.IsDefault && p.Enabled)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Microsoft Graph API için OAuth 2.0 access token alır
    /// </summary>
    private async Task<string> GetGraphApiTokenAsync(SmtpProfile profile)
    {
        var app = ConfidentialClientApplicationBuilder
            .Create(profile.ClientId)
            .WithClientSecret(profile.ClientSecret)
            .WithAuthority($"https://login.microsoftonline.com/{profile.TenantId}")
            .Build();

        // Microsoft Graph API için scope
        var scopes = new[] { "https://graph.microsoft.com/.default" };

        var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

        return result.AccessToken;
    }

    /// <summary>
    /// SMTP OAuth 2.0 için access token alır
    /// </summary>
    private async Task<string> GetSmtpOAuthTokenAsync(SmtpProfile profile)
    {
        var app = ConfidentialClientApplicationBuilder
            .Create(profile.ClientId)
            .WithClientSecret(profile.ClientSecret)
            .WithAuthority($"https://login.microsoftonline.com/{profile.TenantId}")
            .Build();

        // Microsoft 365 SMTP için scope
        var scopes = new[] { "https://outlook.office365.com/.default" };

        var result = await app.AcquireTokenForClient(scopes).ExecuteAsync();

        return result.AccessToken;
    }
}
