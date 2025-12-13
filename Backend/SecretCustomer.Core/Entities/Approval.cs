using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Entities;

/// <summary>
/// Onay kaydı entity'si
/// </summary>
public class Approval : BaseEntity
{
    /// <summary>
    /// Referans numarası
    /// </summary>
    public string ReferenceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Onay türü
    /// </summary>
    public ApprovalType ApprovalType { get; set; } = ApprovalType.General;

    /// <summary>
    /// Durum
    /// </summary>
    public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

    /// <summary>
    /// Başlık
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Açıklama
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// İlişkili kayıt ID (generic)
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// İlişkili kayıt türü
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Talep eden kullanıcı ID
    /// </summary>
    public Guid RequestedByUserId { get; set; }

    /// <summary>
    /// Talep eden kullanıcı
    /// </summary>
    public User RequestedByUser { get; set; } = null!;

    /// <summary>
    /// Onaylayacak kullanıcı ID
    /// </summary>
    public Guid? ApproverUserId { get; set; }

    /// <summary>
    /// Onaylayacak kullanıcı
    /// </summary>
    public User? ApproverUser { get; set; }

    /// <summary>
    /// Onaylayan kullanıcı ID (gerçekleşen)
    /// </summary>
    public Guid? ApprovedByUserId { get; set; }

    /// <summary>
    /// Onaylayan kullanıcı
    /// </summary>
    public User? ApprovedByUser { get; set; }

    /// <summary>
    /// Talep tarihi
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gerekli onay tarihi (son tarih)
    /// </summary>
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Yanıt tarihi
    /// </summary>
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// Onay/ret nedeni
    /// </summary>
    public string? ResponseNote { get; set; }

    /// <summary>
    /// Öncelik
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Otomatik onay süresi (saat)
    /// </summary>
    public int? AutoApproveHours { get; set; }

    /// <summary>
    /// Onay seviyesi (çoklu onay için)
    /// </summary>
    public int ApprovalLevel { get; set; } = 1;

    /// <summary>
    /// Gerekli toplam onay seviyesi
    /// </summary>
    public int RequiredApprovalLevels { get; set; } = 1;

    /// <summary>
    /// JSON formatında ek veri
    /// </summary>
    public string? AdditionalData { get; set; }
}

/// <summary>
/// Bildirim entity'si
/// </summary>
public class Notification : BaseEntity
{
    /// <summary>
    /// Bildirim türü
    /// </summary>
    public NotificationType NotificationType { get; set; } = NotificationType.Info;

    /// <summary>
    /// Kanal
    /// </summary>
    public NotificationChannel Channel { get; set; } = NotificationChannel.InApp;

    /// <summary>
    /// Öncelik
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Başlık
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Mesaj
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Alıcı kullanıcı ID
    /// </summary>
    public Guid RecipientUserId { get; set; }

    /// <summary>
    /// Alıcı kullanıcı
    /// </summary>
    public User RecipientUser { get; set; } = null!;

    /// <summary>
    /// Gönderen kullanıcı ID
    /// </summary>
    public Guid? SenderUserId { get; set; }

    /// <summary>
    /// Gönderen kullanıcı
    /// </summary>
    public User? SenderUser { get; set; }

    /// <summary>
    /// İlişkili kayıt ID
    /// </summary>
    public Guid? RelatedEntityId { get; set; }

    /// <summary>
    /// İlişkili kayıt türü
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Yönlendirme URL'i
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Okundu mu?
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Okunma tarihi
    /// </summary>
    public DateTime? ReadAt { get; set; }

    /// <summary>
    /// Gönderildi mi?
    /// </summary>
    public bool IsSent { get; set; } = false;

    /// <summary>
    /// Gönderim tarihi
    /// </summary>
    public DateTime? SentAt { get; set; }

    /// <summary>
    /// Gönderim başarısız mı?
    /// </summary>
    public bool SendFailed { get; set; } = false;

    /// <summary>
    /// Hata mesajı
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Planlanan gönderim tarihi
    /// </summary>
    public DateTime? ScheduledAt { get; set; }

    /// <summary>
    /// Geçerlilik süresi
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Grup ID (toplu bildirimler için)
    /// </summary>
    public string? GroupId { get; set; }

    /// <summary>
    /// JSON formatında ek veri
    /// </summary>
    public string? AdditionalData { get; set; }
}

/// <summary>
/// Bildirim ayarları entity'si
/// </summary>
public class NotificationSetting : BaseEntity
{
    /// <summary>
    /// Kullanıcı ID
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Kullanıcı
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Bildirim türü
    /// </summary>
    public NotificationType NotificationType { get; set; }

    /// <summary>
    /// Uygulama içi bildirimi aktif mi?
    /// </summary>
    public bool InAppEnabled { get; set; } = true;

    /// <summary>
    /// E-posta bildirimi aktif mi?
    /// </summary>
    public bool EmailEnabled { get; set; } = true;

    /// <summary>
    /// SMS bildirimi aktif mi?
    /// </summary>
    public bool SmsEnabled { get; set; } = false;

    /// <summary>
    /// Push bildirimi aktif mi?
    /// </summary>
    public bool PushEnabled { get; set; } = true;
}
