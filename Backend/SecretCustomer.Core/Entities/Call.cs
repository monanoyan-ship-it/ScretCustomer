using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Çağrı kaydı entity'si
/// </summary>
public class Call : BaseEntity
{
    /// <summary>
    /// Çağrı referans numarası
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Çağrı tipi
    /// </summary>
    public CallType CallType { get; set; } = CallType.Inbound;

    /// <summary>
    /// Çağrı durumu
    /// </summary>
    public CallStatus Status { get; set; } = CallStatus.Pending;

    /// <summary>
    /// Öncelik
    /// </summary>
    public CallPriority Priority { get; set; } = CallPriority.Normal;

    /// <summary>
    /// Konu
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Arayan telefon numarası
    /// </summary>
    public string? CallerPhone { get; set; }

    /// <summary>
    /// Arayan adı
    /// </summary>
    public string? CallerName { get; set; }

    /// <summary>
    /// Aranan telefon numarası
    /// </summary>
    public string? CalledPhone { get; set; }

    /// <summary>
    /// Planlanan çağrı tarihi
    /// </summary>
    public DateTime? ScheduledDate { get; set; }

    /// <summary>
    /// Çağrı başlangıç tarihi/saati
    /// </summary>
    public DateTime? StartTime { get; set; }

    /// <summary>
    /// Çağrı bitiş tarihi/saati
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// Çağrı süresi (saniye)
    /// </summary>
    public int? DurationSeconds { get; set; }

    /// <summary>
    /// Bekleme süresi (saniye)
    /// </summary>
    public int? WaitTimeSeconds { get; set; }

    /// <summary>
    /// Çağrı notları
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Çağrı sonucu
    /// </summary>
    public string? Outcome { get; set; }

    /// <summary>
    /// Çağrı kaydı dosya yolu
    /// </summary>
    public string? RecordingPath { get; set; }

    /// <summary>
    /// Çağrı kaydı dosya adı
    /// </summary>
    public string? RecordingFileName { get; set; }

    /// <summary>
    /// Harici çağrı ID (santral sisteminden)
    /// </summary>
    public string? ExternalCallId { get; set; }

    /// <summary>
    /// İlişkili proje ID
    /// </summary>
    public int? ProjectId { get; set; }

    /// <summary>
    /// İlişkili proje
    /// </summary>
    public Project? Project { get; set; }

    /// <summary>
    /// İlişkili müşteri ID
    /// </summary>
    public int? CustomerId { get; set; }

    /// <summary>
    /// İlişkili müşteri
    /// </summary>
    public Customer? Customer { get; set; }

    /// <summary>
    /// İlişkili şube ID
    /// </summary>
    public int? BranchId { get; set; }

    /// <summary>
    /// İlişkili şube
    /// </summary>
    public Branch? Branch { get; set; }

    /// <summary>
    /// Atanan kullanıcı ID (çağrıyı alan/yapan)
    /// </summary>
    public int? AssignedUserId { get; set; }

    /// <summary>
    /// Atanan kullanıcı
    /// </summary>
    public User? AssignedUser { get; set; }

    /// <summary>
    /// Oluşturan kullanıcı ID
    /// </summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>
    /// Oluşturan kullanıcı
    /// </summary>
    public User? CreatedByUser { get; set; }

    /// <summary>
    /// İlişkili değerlendirme ID
    /// </summary>
    public int? EvaluationId { get; set; }

    /// <summary>
    /// İlişkili değerlendirme
    /// </summary>
    public Evaluation? Evaluation { get; set; }

    /// <summary>
    /// Etiketler (virgülle ayrılmış)
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// Geri arama gerekli mi?
    /// </summary>
    public bool RequiresCallback { get; set; } = false;

    /// <summary>
    /// Geri arama tarihi
    /// </summary>
    public DateTime? CallbackDate { get; set; }

    /// <summary>
    /// Geri arama notu
    /// </summary>
    public string? CallbackNote { get; set; }

    /// <summary>
    /// Geri arama yapıldı mı?
    /// </summary>
    public bool CallbackCompleted { get; set; } = false;

    /// <summary>
    /// Çağrı ekleri
    /// </summary>
    public ICollection<CallAttachment> Attachments { get; set; } = new List<CallAttachment>();
}

/// <summary>
/// Çağrı eki
/// </summary>
public class CallAttachment : BaseEntity
{
    /// <summary>
    /// Çağrı ID
    /// </summary>
    public int CallId { get; set; }

    /// <summary>
    /// Çağrı
    /// </summary>
    public Call Call { get; set; } = null!;

    /// <summary>
    /// Dosya adı
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Dosya yolu
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Dosya boyutu (bytes)
    /// </summary>
    public long FileSize { get; set; }

    /// <summary>
    /// MIME tipi
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Yükleyen kullanıcı ID
    /// </summary>
    public int? UploadedByUserId { get; set; }

    /// <summary>
    /// Yükleyen kullanıcı
    /// </summary>
    public User? UploadedByUser { get; set; }
}
