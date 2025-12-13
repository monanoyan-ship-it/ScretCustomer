namespace SecretCustomer.Core.Enums;

/// <summary>
/// Eğitim tipi
/// </summary>
public enum TrainingType
{
    /// <summary>
    /// Yüz yüze eğitim
    /// </summary>
    InPerson = 0,

    /// <summary>
    /// Online eğitim
    /// </summary>
    Online = 1,

    /// <summary>
    /// Hibrit eğitim
    /// </summary>
    Hybrid = 2,

    /// <summary>
    /// Kendi kendine öğrenme
    /// </summary>
    SelfPaced = 3,

    /// <summary>
    /// İş başı eğitim
    /// </summary>
    OnTheJob = 4,

    /// <summary>
    /// Workshop
    /// </summary>
    Workshop = 5,

    /// <summary>
    /// Seminer
    /// </summary>
    Seminar = 6,

    /// <summary>
    /// Sertifika programı
    /// </summary>
    Certification = 7
}

/// <summary>
/// Eğitim durumu
/// </summary>
public enum TrainingStatus
{
    /// <summary>
    /// Taslak
    /// </summary>
    Draft = 0,

    /// <summary>
    /// Planlandı
    /// </summary>
    Planned = 1,

    /// <summary>
    /// Onay bekliyor
    /// </summary>
    PendingApproval = 2,

    /// <summary>
    /// Onaylandı
    /// </summary>
    Approved = 3,

    /// <summary>
    /// Devam ediyor
    /// </summary>
    InProgress = 4,

    /// <summary>
    /// Tamamlandı
    /// </summary>
    Completed = 5,

    /// <summary>
    /// İptal edildi
    /// </summary>
    Cancelled = 6,

    /// <summary>
    /// Ertelendi
    /// </summary>
    Postponed = 7
}

/// <summary>
/// Eğitim kategorisi
/// </summary>
public enum TrainingCategory
{
    /// <summary>
    /// Genel
    /// </summary>
    General = 0,

    /// <summary>
    /// Teknik
    /// </summary>
    Technical = 1,

    /// <summary>
    /// Satış
    /// </summary>
    Sales = 2,

    /// <summary>
    /// Müşteri hizmetleri
    /// </summary>
    CustomerService = 3,

    /// <summary>
    /// Yönetim
    /// </summary>
    Management = 4,

    /// <summary>
    /// İletişim
    /// </summary>
    Communication = 5,

    /// <summary>
    /// Ürün
    /// </summary>
    Product = 6,

    /// <summary>
    /// Süreç
    /// </summary>
    Process = 7,

    /// <summary>
    /// Güvenlik
    /// </summary>
    Safety = 8,

    /// <summary>
    /// Uyum
    /// </summary>
    Compliance = 9
}

/// <summary>
/// Katılımcı durumu
/// </summary>
public enum TrainingParticipantStatus
{
    /// <summary>
    /// Davetli
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
    /// Katıldı
    /// </summary>
    Attended = 3,

    /// <summary>
    /// Katılmadı
    /// </summary>
    NotAttended = 4,

    /// <summary>
    /// Tamamladı
    /// </summary>
    Completed = 5,

    /// <summary>
    /// Başarısız
    /// </summary>
    Failed = 6
}
