namespace SecretCustomer.Core.Enums;

/// <summary>
/// Proje durumu
/// </summary>
public enum ProjectStatus
{
    /// <summary>
    /// Taslak - Henuz baslatilmadi
    /// </summary>
    Draft = 1,

    /// <summary>
    /// Planlanmis - Baslangic tarihi bekleniyor
    /// </summary>
    Planned = 2,

    /// <summary>
    /// Aktif - Devam ediyor
    /// </summary>
    Active = 3,

    /// <summary>
    /// Duraklatildi - Gecici olarak durduruldu
    /// </summary>
    Paused = 4,

    /// <summary>
    /// Tamamlandi - Basariyla bitti
    /// </summary>
    Completed = 5,

    /// <summary>
    /// Iptal Edildi
    /// </summary>
    Cancelled = 6
}
