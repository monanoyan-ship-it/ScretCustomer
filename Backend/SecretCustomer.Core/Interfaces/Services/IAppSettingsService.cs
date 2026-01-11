using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IAppSettingsService
{
    // Temel CRUD
    Task<AppSettings?> GetByKeyAsync(string key, int? entityId = null, string? entityType = null);
    Task<IEnumerable<AppSettings>> GetByCategoryAsync(string category);
    Task<IEnumerable<AppSettings>> GetAllAsync();
    Task<IEnumerable<AppSettings>> GetByEntityAsync(int entityId, string entityType);
    Task<AppSettings> SetAsync(string key, string value, int valueTypeId = SettingValueTypes.Ids.String,
        string category = "General", string? description = null, int? entityId = null, string? entityType = null);
    Task DeleteAsync(string key, int? entityId = null, string? entityType = null);

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
