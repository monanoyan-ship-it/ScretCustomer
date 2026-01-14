namespace SecretCustomer.Core.Interfaces.Services;

/// <summary>
/// Email gönderme servisi interface
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Tek alıcıya mail gönder
    /// </summary>
    Task<EmailResult> SendEmailAsync(string to, string subject, string body, bool isHtml = true);

    /// <summary>
    /// Birden fazla alıcıya mail gönder
    /// </summary>
    Task<EmailResult> SendEmailAsync(IEnumerable<string> to, string subject, string body, bool isHtml = true);

    /// <summary>
    /// Ek dosyalı mail gönder
    /// </summary>
    Task<EmailResult> SendEmailWithAttachmentAsync(string to, string subject, string body, List<EmailAttachment> attachments, bool isHtml = true);

    /// <summary>
    /// CC ve BCC destekli mail gönder
    /// </summary>
    Task<EmailResult> SendEmailAsync(EmailMessage message);

    /// <summary>
    /// SMTP bağlantısını test et
    /// </summary>
    Task<EmailResult> TestConnectionAsync();

    /// <summary>
    /// SMTP ayarlarının yapılandırılıp yapılandırılmadığını kontrol et
    /// </summary>
    Task<bool> IsConfiguredAsync();
}

/// <summary>
/// Email gönderim sonucu
/// </summary>
public class EmailResult
{
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MessageId { get; set; }

    public static EmailResult Ok(string? messageId = null) => new() { Success = true, MessageId = messageId };
    public static EmailResult Fail(string error) => new() { Success = false, ErrorMessage = error };
}

/// <summary>
/// Email mesajı modeli
/// </summary>
public class EmailMessage
{
    public List<string> To { get; set; } = new();
    public List<string> Cc { get; set; } = new();
    public List<string> Bcc { get; set; } = new();
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsHtml { get; set; } = true;
    public List<EmailAttachment> Attachments { get; set; } = new();
    public string? ReplyTo { get; set; }
}

/// <summary>
/// Email eki modeli
/// </summary>
public class EmailAttachment
{
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = "application/octet-stream";
}
