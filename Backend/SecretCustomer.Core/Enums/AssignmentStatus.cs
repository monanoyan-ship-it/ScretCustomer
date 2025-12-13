namespace SecretCustomer.Core.Enums;

/// <summary>
/// Görev atama durumları
/// </summary>
public enum AssignmentStatus
{
    /// <summary>
    /// Beklemede - Henüz başlanmadı
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Devam Ediyor - Değerlendirme başladı
    /// </summary>
    InProgress = 2,

    /// <summary>
    /// Tamamlandı - Değerlendirme tamamlandı
    /// </summary>
    Completed = 3,

    /// <summary>
    /// Süresi Dolmuş - Due date geçmiş
    /// </summary>
    Expired = 4,

    /// <summary>
    /// İptal Edildi
    /// </summary>
    Cancelled = 5
}
