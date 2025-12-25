using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Müşteri/Bayi Ziyaret Kaydı
/// </summary>
public class CustomerVisit : BaseEntity
{
    /// <summary>
    /// Ziyaret edilen müşteri
    /// </summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>
    /// Ziyaret edilen şube
    /// </summary>
    public int? BranchId { get; set; }
    public Branch? Branch { get; set; }

    /// <summary>
    /// Ziyareti yapan kullanıcı
    /// </summary>
    public int VisitorUserId { get; set; }
    public User VisitorUser { get; set; } = null!;

    /// <summary>
    /// Ziyaret tipi
    /// </summary>
    public VisitType VisitType { get; set; } = VisitType.Routine;

    /// <summary>
    /// Ziyaret durumu
    /// </summary>
    public VisitStatus Status { get; set; } = VisitStatus.Planned;

    /// <summary>
    /// Planlanan ziyaret tarihi
    /// </summary>
    public DateTime PlannedDate { get; set; }

    /// <summary>
    /// Planlanan saat
    /// </summary>
    public string? PlannedTime { get; set; }

    /// <summary>
    /// Gerçekleşen ziyaret tarihi
    /// </summary>
    public DateTime? ActualDate { get; set; }

    /// <summary>
    /// Ziyaret başlangıç saati
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Ziyaret bitiş saati
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Ziyaret süresi (dakika)
    /// </summary>
    public int? DurationMinutes { get; set; }

    /// <summary>
    /// Ziyaret amacı
    /// </summary>
    public string? Purpose { get; set; }

    /// <summary>
    /// Ziyaret notları
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gözlemler
    /// </summary>
    public string? Observations { get; set; }

    /// <summary>
    /// Görüşülen kişi adı
    /// </summary>
    public string? ContactPersonName { get; set; }

    /// <summary>
    /// Görüşülen kişi pozisyonu
    /// </summary>
    public string? ContactPersonTitle { get; set; }

    /// <summary>
    /// Görüşülen kişi telefonu
    /// </summary>
    public string? ContactPersonPhone { get; set; }

    /// <summary>
    /// Adres (ziyaret adresi)
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// GPS Enlem
    /// </summary>
    public double? Latitude { get; set; }

    /// <summary>
    /// GPS Boylam
    /// </summary>
    public double? Longitude { get; set; }

    /// <summary>
    /// Sonraki aksiyon gerekli mi?
    /// </summary>
    public bool RequiresFollowUp { get; set; } = false;

    /// <summary>
    /// Sonraki aksiyon açıklaması
    /// </summary>
    public string? FollowUpNotes { get; set; }

    /// <summary>
    /// Sonraki aksiyon tarihi
    /// </summary>
    public DateTime? FollowUpDate { get; set; }

    /// <summary>
    /// İlişkili proje
    /// </summary>
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }

    /// <summary>
    /// İlişkili değerlendirme
    /// </summary>
    public int? EvaluationId { get; set; }
    public Evaluation? Evaluation { get; set; }

    // Navigation properties
    public ICollection<CustomerVisitAttachment> Attachments { get; set; } = new List<CustomerVisitAttachment>();

    /// <summary>
    /// Dinamik ziyaret detayları
    /// </summary>
    public ICollection<VisitDetailValue> DetailValues { get; set; } = new List<VisitDetailValue>();
}

/// <summary>
/// Ziyaret eki (fotoğraf, belge vb.)
/// </summary>
public class CustomerVisitAttachment : BaseEntity
{
    public int CustomerVisitId { get; set; }
    public CustomerVisit CustomerVisit { get; set; } = null!;

    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Order { get; set; }

    /// <summary>
    /// Dosya tipi (Photo, Document, Other)
    /// </summary>
    public AttachmentType AttachmentType { get; set; } = AttachmentType.Document;
}
