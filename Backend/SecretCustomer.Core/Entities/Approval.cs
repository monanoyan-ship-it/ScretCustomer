using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;

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
    /// Onay türü (ApprovalTypes.Ids kullanılır)
    /// </summary>
    public int ApprovalTypeId { get; set; } = ApprovalTypes.Ids.General;

    /// <summary>
    /// Durum (ApprovalStatuses.Ids kullanılır)
    /// </summary>
    public int StatusId { get; set; } = ApprovalStatuses.Ids.Pending;

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
    public int? RelatedEntityId { get; set; }

    /// <summary>
    /// İlişkili kayıt türü
    /// </summary>
    public string? RelatedEntityType { get; set; }

    /// <summary>
    /// Talep eden kullanıcı ID (Users tablosu - admin/evaluator)
    /// </summary>
    public int? RequestedByUserId { get; set; }

    /// <summary>
    /// Talep eden kullanıcı
    /// </summary>
    public User? RequestedByUser { get; set; }

    /// <summary>
    /// Talep eden müşteri personeli ID (CustomerPersonnel tablosu)
    /// </summary>
    public int? RequestedByCustomerPersonnelId { get; set; }

    /// <summary>
    /// Talep eden müşteri personeli
    /// </summary>
    public CustomerPersonnel? RequestedByCustomerPersonnel { get; set; }

    /// <summary>
    /// Onaylayacak kullanıcı ID
    /// </summary>
    public int? ApproverUserId { get; set; }

    /// <summary>
    /// Onaylayacak kullanıcı
    /// </summary>
    public User? ApproverUser { get; set; }

    /// <summary>
    /// Onaylayan kullanıcı ID (gerçekleşen)
    /// </summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>
    /// Onaylayan kullanıcı
    /// </summary>
    public User? ApprovedByUser { get; set; }

    /// <summary>
    /// Talep tarihi
    /// </summary>
    public DateTime RequestedAt { get; set; } = TurkeyTime.Now;

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
    /// Öncelik (NotificationPriorities.Ids kullanılır)
    /// </summary>
    public int PriorityId { get; set; } = NotificationPriorities.Ids.Normal;

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
    /// Bildirim türü (NotificationTypes.Ids kullanılır)
    /// </summary>
    public int NotificationTypeId { get; set; } = NotificationTypes.Ids.Info;

    /// <summary>
    /// Kanal (NotificationChannels.Ids kullanılır)
    /// </summary>
    public int ChannelId { get; set; } = NotificationChannels.Ids.InApp;

    /// <summary>
    /// Öncelik (NotificationPriorities.Ids kullanılır)
    /// </summary>
    public int PriorityId { get; set; } = NotificationPriorities.Ids.Normal;

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
    public int RecipientUserId { get; set; }

    /// <summary>
    /// Alıcı kullanıcı
    /// </summary>
    public User RecipientUser { get; set; } = null!;

    /// <summary>
    /// Gönderen kullanıcı ID
    /// </summary>
    public int? SenderUserId { get; set; }

    /// <summary>
    /// Gönderen kullanıcı
    /// </summary>
    public User? SenderUser { get; set; }

    /// <summary>
    /// İlişkili kayıt ID
    /// </summary>
    public int? RelatedEntityId { get; set; }

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
    public int UserId { get; set; }

    /// <summary>
    /// Kullanıcı
    /// </summary>
    public User User { get; set; } = null!;

    /// <summary>
    /// Bildirim türü (NotificationTypes.Ids kullanılır)
    /// </summary>
    public int NotificationTypeId { get; set; }

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
