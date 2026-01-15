namespace SecretCustomer.Core.Entities;

/// <summary>
/// Proje tipi bazlı performans ayarları
/// Her proje tipi için günlük/haftalık/aylık hedefler ve puan eşikleri
/// </summary>
public class PerformanceSettings : BaseEntity
{
    /// <summary>
    /// Proje tipi ID (ProjectTypes.Ids)
    /// </summary>
    public int ProjectTypeId { get; set; }

    /// <summary>
    /// Günlük değerlendirme hedefi
    /// </summary>
    public int? DailyTarget { get; set; }

    /// <summary>
    /// Haftalık değerlendirme hedefi
    /// </summary>
    public int? WeeklyTarget { get; set; }

    /// <summary>
    /// Aylık değerlendirme hedefi
    /// </summary>
    public int? MonthlyTarget { get; set; }

    /// <summary>
    /// Yıllık değerlendirme hedefi
    /// </summary>
    public int? YearlyTarget { get; set; }

    /// <summary>
    /// Başarılı performans alt sınırı (%)
    /// </summary>
    public decimal? SuccessThreshold { get; set; } = 80;

    /// <summary>
    /// Orta performans alt sınırı (%)
    /// </summary>
    public decimal? WarningThreshold { get; set; } = 60;

    /// <summary>
    /// Aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;
}
