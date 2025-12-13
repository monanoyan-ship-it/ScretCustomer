namespace SecretCustomer.Core.Entities;

/// <summary>
/// Sorulara eklenen dosyalar
/// </summary>
public class QuestionAttachment : BaseEntity
{
    /// <summary>
    /// Soru ID
    /// </summary>
    public Guid QuestionId { get; set; }
    public Question Question { get; set; } = null!;

    /// <summary>
    /// Orijinal dosya adı
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Sunucudaki dosya adı (unique)
    /// </summary>
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// Dosya yolu (göreceli)
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Dosya boyutu (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME tipi
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Dosya açıklaması
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Sıralama
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Yükleyen kullanıcı
    /// </summary>
    public Guid? UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }
}
