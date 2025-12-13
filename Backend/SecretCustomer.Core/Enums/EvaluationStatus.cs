namespace SecretCustomer.Core.Enums;

/// <summary>
/// Değerlendirme durumu
/// </summary>
public enum EvaluationStatus
{
    /// <summary>
    /// Beklemede - Henüz başlanmadı
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Devam ediyor - Değerlendirme yapılıyor
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Tamamlandı - Değerlendirme bitti
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Taslak - Kaydedildi ama tamamlanmadı
    /// </summary>
    Draft = 4,

    /// <summary>
    /// İptal edildi
    /// </summary>
    Cancelled = 5
}
