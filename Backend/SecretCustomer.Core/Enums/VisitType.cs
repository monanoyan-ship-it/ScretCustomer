namespace SecretCustomer.Core.Enums;

/// <summary>
/// Ziyaret tipleri
/// </summary>
public enum VisitType
{
    /// <summary>
    /// Rutin ziyaret
    /// </summary>
    Routine = 0,

    /// <summary>
    /// İlk ziyaret
    /// </summary>
    Initial = 1,

    /// <summary>
    /// Takip ziyareti
    /// </summary>
    FollowUp = 2,

    /// <summary>
    /// Denetim ziyareti
    /// </summary>
    Audit = 3,

    /// <summary>
    /// Eğitim ziyareti
    /// </summary>
    Training = 4,

    /// <summary>
    /// Şikayet/Sorun çözümü
    /// </summary>
    Complaint = 5,

    /// <summary>
    /// Satış ziyareti
    /// </summary>
    Sales = 6,

    /// <summary>
    /// Gizli müşteri ziyareti
    /// </summary>
    MysteryShop = 7,

    /// <summary>
    /// Banka gizli müşteri ziyareti (GBF)
    /// </summary>
    BankMysteryShop = 8,

    /// <summary>
    /// Diğer
    /// </summary>
    Other = 99
}

/// <summary>
/// Ziyaret durumları
/// </summary>
public enum VisitStatus
{
    /// <summary>
    /// Planlandı
    /// </summary>
    Planned = 0,

    /// <summary>
    /// Devam ediyor
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Tamamlandı
    /// </summary>
    Completed = 2,

    /// <summary>
    /// İptal edildi
    /// </summary>
    Cancelled = 3,

    /// <summary>
    /// Ertelendi
    /// </summary>
    Postponed = 4,

    /// <summary>
    /// Gerçekleştirilemedi
    /// </summary>
    NotCompleted = 5
}

/// <summary>
/// Ek dosya tipleri
/// </summary>
public enum AttachmentType
{
    /// <summary>
    /// Fotoğraf
    /// </summary>
    Photo = 0,

    /// <summary>
    /// Belge
    /// </summary>
    Document = 1,

    /// <summary>
    /// Video
    /// </summary>
    Video = 2,

    /// <summary>
    /// Ses kaydı
    /// </summary>
    Audio = 3,

    /// <summary>
    /// Diğer
    /// </summary>
    Other = 99
}
