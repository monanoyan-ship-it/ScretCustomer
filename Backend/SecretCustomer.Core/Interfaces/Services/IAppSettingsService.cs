using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IAppSettingsService
{
    // Temel CRUD
    Task<AppSettings?> GetByKeyAsync(string key, Guid? entityId = null, string? entityType = null);
    Task<IEnumerable<AppSettings>> GetByCategoryAsync(string category);
    Task<IEnumerable<AppSettings>> GetAllAsync();
    Task<IEnumerable<AppSettings>> GetByEntityAsync(Guid entityId, string entityType);
    Task<AppSettings> SetAsync(string key, string value, SettingValueType valueType = SettingValueType.String,
        string category = "General", string? description = null, Guid? entityId = null, string? entityType = null);
    Task DeleteAsync(string key, Guid? entityId = null, string? entityType = null);

    // Typed getters
    Task<string?> GetStringAsync(string key, string? defaultValue = null);
    Task<bool> GetBoolAsync(string key, bool defaultValue = false);
    Task<int> GetIntAsync(string key, int defaultValue = 0);
    Task<decimal> GetDecimalAsync(string key, decimal defaultValue = 0);
    Task<T?> GetJsonAsync<T>(string key) where T : class;

    // Sık kullanılan ayarlar için helper metodlar
    Task<bool> IsDemoModeAsync();
    Task<bool> IsMaintenanceModeAsync();
}
