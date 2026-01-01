using SecretCustomer.Core.DTOs.SystemSetting;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ISystemSettingService
{
    Task<IEnumerable<SystemSettingDto>> GetAllAsync();
    Task<SystemSettingDto?> GetByKeyAsync(string key);
    Task<string?> GetValueAsync(string key);
    Task<int> GetIntValueAsync(string key, int defaultValue = 0);
    Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue = 0);
    Task<bool> GetBoolValueAsync(string key, bool defaultValue = false);
    Task<SystemSettingDto> CreateAsync(CreateSystemSettingDto dto);
    Task<SystemSettingDto> UpdateAsync(string key, UpdateSystemSettingDto dto);
    Task<bool> SetValueAsync(string key, string value);
    Task<bool> DeleteAsync(string key);
}
