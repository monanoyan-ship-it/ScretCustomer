namespace SecretCustomer.Core.Enums;

/// <summary>
/// Toplantı tipi
/// </summary>
public enum MeetingType
{
    /// <summary>
    /// Genel toplantı
    /// </summary>
    General = 0,

    /// <summary>
    /// Proje toplantısı
    /// </summary>
    Project = 1,

    /// <summary>
    /// Değerlendirme toplantısı
    /// </summary>
    Evaluation = 2,

    /// <summary>
    /// Eğitim toplantısı
    /// </summary>
    Training = 3,

    /// <summary>
    /// Müşteri toplantısı
    /// </summary>
    Customer = 4,

    /// <summary>
    /// Kick-off toplantısı
    /// </summary>
    KickOff = 5,

    /// <summary>
    /// Kapanış toplantısı
    /// </summary>
    Closing = 6
}

/// <summary>
/// Toplantı durumu
/// </summary>
public enum MeetingStatus
{
    /// <summary>
    /// Planlandı
    /// </summary>
    Planned = 0,

    /// <summary>
    /// Onay bekliyor
    /// </summary>
    PendingApproval = 1,

    /// <summary>
    /// Onaylandı
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Devam ediyor
    /// </summary>
    InProgress = 3,

    /// <summary>
    /// Tamamlandı
    /// </summary>
    Completed = 4,

    /// <summary>
    /// İptal edildi
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Ertelendi
    /// </summary>
    Postponed = 6
}

/// <summary>
/// Katılımcı durumu
/// </summary>
public enum ParticipantStatus
{
    /// <summary>
    /// Davet edildi
    /// </summary>
    Invited = 0,

    /// <summary>
    /// Kabul etti
    /// </summary>
    Accepted = 1,

    /// <summary>
    /// Reddetti
    /// </summary>
    Declined = 2,

    /// <summary>
    /// Belirsiz
    /// </summary>
    Tentative = 3,

    /// <summary>
    /// Katıldı
    /// </summary>
    Attended = 4,

    /// <summary>
    /// Katılmadı
    /// </summary>
    NotAttended = 5
}
