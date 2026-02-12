using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.SystemSetting;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services;

public class SystemSettingService : ISystemSettingService
{
    private readonly ApplicationDbContext _context;

    public SystemSettingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SystemSettingDto>> GetAllAsync()
    {
        return await _context.SystemSettings
            .Where(s => !s.IsDeleted)
            .OrderBy(s => s.Category)
            .ThenBy(s => s.Key)
            .Select(s => MapToDto(s))
            .ToListAsync();
    }

    public async Task<SystemSettingDto?> GetByKeyAsync(string key)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted);

        return setting == null ? null : MapToDto(setting);
    }

    public async Task<string?> GetValueAsync(string key)
    {
        return await _context.SystemSettings
            .Where(s => s.Key == key && !s.IsDeleted)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();
    }

    public async Task<int> GetIntValueAsync(string key, int defaultValue = 0)
    {
        var value = await GetValueAsync(key);
        return int.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<decimal> GetDecimalValueAsync(string key, decimal defaultValue = 0)
    {
        var value = await GetValueAsync(key);
        return decimal.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<bool> GetBoolValueAsync(string key, bool defaultValue = false)
    {
        var value = await GetValueAsync(key);
        return bool.TryParse(value, out var result) ? result : defaultValue;
    }

    public async Task<SystemSettingDto> CreateAsync(CreateSystemSettingDto dto)
    {
        // Check if key already exists
        var existing = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == dto.Key && !s.IsDeleted);

        if (existing != null)
            throw new InvalidOperationException($"Setting with key '{dto.Key}' already exists");

        var setting = new SystemSetting
        {
            Key = dto.Key,
            Value = dto.Value,
            Description = dto.Description,
            ValueType = dto.ValueType,
            Category = dto.Category
        };

        _context.SystemSettings.Add(setting);
        await _context.SaveChangesAsync();

        return MapToDto(setting);
    }

    public async Task<SystemSettingDto> UpdateAsync(string key, UpdateSystemSettingDto dto)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted);

        if (setting == null)
            throw new KeyNotFoundException($"Setting with key '{key}' not found");

        setting.Value = dto.Value;
        if (dto.Description != null)
            setting.Description = dto.Description;
        setting.UpdatedAt = TurkeyTime.Now;

        await _context.SaveChangesAsync();

        return MapToDto(setting);
    }

    public async Task<bool> SetValueAsync(string key, string value)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted);

        if (setting == null)
        {
            // Create new setting
            setting = new SystemSetting
            {
                Key = key,
                Value = value
            };
            _context.SystemSettings.Add(setting);
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = TurkeyTime.Now;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(string key)
    {
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == key && !s.IsDeleted);

        if (setting == null)
            return false;

        setting.IsDeleted = true;
        setting.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        return true;
    }

    private static SystemSettingDto MapToDto(SystemSetting setting)
    {
        return new SystemSettingDto
        {
            Id = setting.Id,
            Key = setting.Key,
            Value = setting.Value,
            Description = setting.Description,
            ValueType = setting.ValueType,
            Category = setting.Category
        };
    }
}
