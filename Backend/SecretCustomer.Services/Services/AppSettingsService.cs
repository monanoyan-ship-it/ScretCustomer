using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Text.Json;

namespace SecretCustomer.Services.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly ApplicationDbContext _context;

    public AppSettingsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppSettings?> GetByKeyAsync(string key, int? entityId = null, string? entityType = null)
    {
        return await _context.AppSettings
            .FirstOrDefaultAsync(s => s.Key == key && s.EntityId == entityId && s.EntityType == entityType);
    }

    public async Task<IEnumerable<AppSettings>> GetByCategoryAsync(string category)
    {
        return await _context.AppSettings
            .Where(s => s.Category == category && s.EntityId == null)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Key)
            .ToListAsync();
    }

    public async Task<IEnumerable<AppSettings>> GetAllAsync()
    {
        return await _context.AppSettings
            .Where(s => s.EntityId == null)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.DisplayOrder)
            .ThenBy(s => s.Key)
            .ToListAsync();
    }

    public async Task<IEnumerable<AppSettings>> GetByEntityAsync(int entityId, string entityType)
    {
        return await _context.AppSettings
            .Where(s => s.EntityId == entityId && s.EntityType == entityType)
            .OrderBy(s => s.DisplayOrder)
            .ThenBy(s => s.Key)
            .ToListAsync();
    }

    public async Task<AppSettings> SetAsync(string key, string value, SettingValueType valueType = SettingValueType.String,
        string category = "General", string? description = null, int? entityId = null, string? entityType = null)
    {
        var setting = await GetByKeyAsync(key, entityId, entityType);

        if (setting == null)
        {
            setting = new AppSettings
            {
                Key = key,
                Value = value,
                ValueType = valueType,
                Category = category,
                Description = description,
                EntityId = entityId,
                EntityType = entityType,
                CreatedAt = DateTime.UtcNow
            };
            _context.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return setting;
    }

    public async Task DeleteAsync(string key, int? entityId = null, string? entityType = null)
    {
        var setting = await GetByKeyAsync(key, entityId, entityType);
        if (setting != null && !setting.IsSystem)
        {
            _context.AppSettings.Remove(setting);
            await _context.SaveChangesAsync();
        }
    }

    // Typed getters
    public async Task<string?> GetStringAsync(string key, string? defaultValue = null)
    {
        var setting = await GetByKeyAsync(key);
        return setting?.Value ?? defaultValue;
    }

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = false)
    {
        var setting = await GetByKeyAsync(key);
        if (setting == null) return defaultValue;
        return bool.TryParse(setting.Value, out var result) ? result : defaultValue;
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
        var setting = await GetByKeyAsync(key);
        if (setting == null) return defaultValue;
        return int.TryParse(setting.Value, out var result) ? result : defaultValue;
    }

    public async Task<decimal> GetDecimalAsync(string key, decimal defaultValue = 0)
    {
        var setting = await GetByKeyAsync(key);
        if (setting == null) return defaultValue;
        return decimal.TryParse(setting.Value, out var result) ? result : defaultValue;
    }

    public async Task<T?> GetJsonAsync<T>(string key) where T : class
    {
        var setting = await GetByKeyAsync(key);
        if (setting == null || string.IsNullOrEmpty(setting.Value)) return null;
        try
        {
            return JsonSerializer.Deserialize<T>(setting.Value);
        }
        catch
        {
            return null;
        }
    }

    // Helper metodlar
    public async Task<bool> IsDemoModeAsync()
    {
        return await GetBoolAsync("General.DemoMode", true); // Default true
    }

    public async Task<bool> IsMaintenanceModeAsync()
    {
        return await GetBoolAsync("General.MaintenanceMode", false);
    }
}
