using SecretCustomer.Core.DTOs.PerformanceSettings;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IPerformanceSettingsService
{
    /// <summary>
    /// Tüm performans ayarlarını getirir (proje tipi bilgisiyle)
    /// </summary>
    Task<List<PerformanceSettingsListDto>> GetAllAsync();

    /// <summary>
    /// Belirli bir proje tipinin performans ayarlarını getirir
    /// </summary>
    Task<PerformanceSettingsDto?> GetByProjectTypeIdAsync(int projectTypeId);

    /// <summary>
    /// Performans ayarlarını kaydeder (varsa günceller, yoksa oluşturur)
    /// </summary>
    Task<PerformanceSettingsDto> SaveAsync(SavePerformanceSettingsDto dto, string? userId = null);

    /// <summary>
    /// Tüm proje tipleri için ayarları toplu kaydeder
    /// </summary>
    Task<List<PerformanceSettingsDto>> BulkSaveAsync(BulkSavePerformanceSettingsDto dto, string? userId = null);

    /// <summary>
    /// Performans ayarlarını siler (soft delete)
    /// </summary>
    Task<bool> DeleteAsync(int projectTypeId, string? userId = null);

    /// <summary>
    /// Aktif performans ayarlarını getirir (PerformanceTracking için)
    /// </summary>
    Task<Dictionary<int, PerformanceSettingsListDto>> GetActiveSettingsAsync();
}
