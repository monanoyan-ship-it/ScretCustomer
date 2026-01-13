namespace SecretCustomer.Core.Entities;

/// <summary>
/// Değerlendirme eki - Değerlendirmeye eklenen dosyalar
/// </summary>
public class EvaluationAttachment : BaseEntity
{
    public int EvaluationId { get; set; }
    public Evaluation Evaluation { get; set; } = null!;

    /// <summary>
    /// Dosya adı (orijinal)
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Dosya yolu (sunucudaki)
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Dosya boyutu (byte)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// İçerik tipi (MIME type)
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Yükleyen kullanıcı ID
    /// </summary>
    public int? UploadedByUserId { get; set; }
    public User? UploadedByUser { get; set; }
}
