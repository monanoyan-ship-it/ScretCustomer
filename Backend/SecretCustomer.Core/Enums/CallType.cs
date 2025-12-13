namespace SecretCustomer.Core.Enums;

/// <summary>
/// Çağrı tipi
/// </summary>
public enum CallType
{
    /// <summary>
    /// Gelen çağrı
    /// </summary>
    Inbound = 0,

    /// <summary>
    /// Giden çağrı
    /// </summary>
    Outbound = 1,

    /// <summary>
    /// Dahili çağrı
    /// </summary>
    Internal = 2,

    /// <summary>
    /// Konferans çağrısı
    /// </summary>
    Conference = 3,

    /// <summary>
    /// Gizli müşteri çağrısı
    /// </summary>
    MysteryCall = 4
}

/// <summary>
/// Çağrı durumu
/// </summary>
public enum CallStatus
{
    /// <summary>
    /// Planlandı
    /// </summary>
    Scheduled = 0,

    /// <summary>
    /// Beklemede
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Devam ediyor
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Tamamlandı
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Cevapsız
    /// </summary>
    Missed = 4,

    /// <summary>
    /// İptal edildi
    /// </summary>
    Cancelled = 5,

    /// <summary>
    /// Başarısız
    /// </summary>
    Failed = 6,

    /// <summary>
    /// Değerlendirildi
    /// </summary>
    Evaluated = 7
}

/// <summary>
/// Çağrı önceliği
/// </summary>
public enum CallPriority
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
