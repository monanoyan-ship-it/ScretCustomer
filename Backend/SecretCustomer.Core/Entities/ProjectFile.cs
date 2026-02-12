using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Projeye eklenen dosyalar
/// </summary>
public class ProjectFile : BaseEntity
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    /// <summary>
    /// Disk'te saklanan dosya adi (GUID.ext)
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Orijinal dosya adi
    /// </summary>
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Dosya yolu (wwwroot/uploads/projects/{projectId}/)
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// MIME tipi
    /// </summary>
    public string ContentType { get; set; } = string.Empty;

    /// <summary>
    /// Dosya boyutu (byte)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// Yukleme tarihi
    /// </summary>
    public DateTime UploadedAt { get; set; } = TurkeyTime.Now;

    /// <summary>
    /// Yukleyen kullanici
    /// </summary>
    public int? UploadedById { get; set; }
    public User? UploadedBy { get; set; }

    /// <summary>
    /// Dosya aciklamasi
    /// </summary>
    public string? Description { get; set; }
}
