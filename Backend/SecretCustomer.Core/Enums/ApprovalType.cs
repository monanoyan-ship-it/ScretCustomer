namespace SecretCustomer.Core.Enums;

/// <summary>
/// Onay türü
/// </summary>
public enum ApprovalType
{
    /// <summary>
    /// Değerlendirme onayı
    /// </summary>
    Evaluation = 0,

    /// <summary>
    /// Atama onayı
    /// </summary>
    Assignment = 1,

    /// <summary>
    /// Proje onayı
    /// </summary>
    Project = 2,

    /// <summary>
    /// Toplantı onayı
    /// </summary>
    Meeting = 3,

    /// <summary>
    /// Eğitim onayı
    /// </summary>
    Training = 4,

    /// <summary>
    /// Rapor onayı
    /// </summary>
    Report = 5,

    /// <summary>
    /// Vekalet onayı
    /// </summary>
    Delegation = 6,

    /// <summary>
    /// Genel onay
    /// </summary>
    General = 7
}

/// <summary>
/// Onay durumu
/// </summary>
public enum ApprovalStatus
{
    /// <summary>
    /// Beklemede
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Onaylandı
    /// </summary>
    Approved = 1,

    /// <summary>
    /// Reddedildi
    /// </summary>
    Rejected = 2,

    /// <summary>
    /// Revizyon istendi
    /// </summary>
    RevisionRequested = 3,

    /// <summary>
    /// İptal edildi
    /// </summary>
    Cancelled = 4,

    /// <summary>
    /// Zaman aşımı
    /// </summary>
    Expired = 5
}

/// <summary>
/// Bildirim türü
/// </summary>
public enum NotificationType
{
    /// <summary>
    /// Bilgi
    /// </summary>
    Info = 0,

    /// <summary>
    /// Başarı
    /// </summary>
    Success = 1,

    /// <summary>
    /// Uyarı
    /// </summary>
    Warning = 2,

    /// <summary>
    /// Hata
    /// </summary>
    Error = 3,

    /// <summary>
    /// Onay talebi
    /// </summary>
    ApprovalRequest = 4,

    /// <summary>
    /// Atama bildirimi
    /// </summary>
    Assignment = 5,

    /// <summary>
    /// Toplantı daveti
    /// </summary>
    MeetingInvite = 6,

    /// <summary>
    /// Eğitim daveti
    /// </summary>
    TrainingInvite = 7,

    /// <summary>
    /// Hatırlatma
    /// </summary>
    Reminder = 8,

    /// <summary>
    /// Sistem bildirimi
    /// </summary>
    System = 9
}

/// <summary>
/// Bildirim kanalı
/// </summary>
public enum NotificationChannel
{
    /// <summary>
    /// Uygulama içi
    /// </summary>
    InApp = 0,

    /// <summary>
    /// E-posta
    /// </summary>
    Email = 1,

    /// <summary>
    /// SMS
    /// </summary>
    Sms = 2,

    /// <summary>
    /// Push bildirimi
    /// </summary>
    Push = 3
}

/// <summary>
/// Bildirim önceliği
/// </summary>
public enum NotificationPriority
{
    /// <summary>
    /// Düşük
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Yüksek
    /// </summary>
    High = 2,

    /// <summary>
    /// Acil
    /// </summary>
    Urgent = 3
}
