using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Sistem duyuruları
/// </summary>
public class Announcement : BaseEntity
{
    /// <summary>
    /// Duyuru başlığı
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Duyuru içeriği (HTML destekli)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Kısa özet (dashboard için)
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Duyuru tipi
    /// </summary>
    public AnnouncementType Type { get; set; } = AnnouncementType.Info;

    /// <summary>
    /// Öncelik seviyesi (1-5, 5 en yüksek)
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// Yayınlanma tarihi
    /// </summary>
    public DateTime PublishDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Bitiş tarihi (opsiyonel)
    /// </summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Sabitlenmiş mi? (Her zaman üstte görünsün)
    /// </summary>
    public bool IsPinned { get; set; } = false;

    /// <summary>
    /// Hedef roller (boşsa herkese gösterilir)
    /// Virgülle ayrılmış roller: "Admin,TeamLeader"
    /// </summary>
    public string? TargetRoles { get; set; }

    /// <summary>
    /// Oluşturan kullanıcı
    /// </summary>
    public int? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
}
