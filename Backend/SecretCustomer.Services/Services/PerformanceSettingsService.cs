using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.PerformanceSettings;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class PerformanceSettingsService : IPerformanceSettingsService
{
    private readonly ApplicationDbContext _context;

    public PerformanceSettingsService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PerformanceSettingsListDto>> GetAllAsync()
    {
        // Veritabanındaki mevcut ayarları al
        var existingSettings = await _context.PerformanceSettings
            .Where(ps => !ps.IsDeleted)
            .ToListAsync();

        var result = new List<PerformanceSettingsListDto>();

        // Tüm proje tipleri için DTO oluştur (ayar yoksa varsayılan değerlerle)
        foreach (var projectType in ProjectTypes.All)
        {
            var existing = existingSettings.FirstOrDefault(s => s.ProjectTypeId == projectType.Id);

            result.Add(new PerformanceSettingsListDto
            {
                Id = existing?.Id ?? 0,
                ProjectTypeId = projectType.Id,
                ProjectTypeName = projectType.Description,
                ProjectTypeIcon = projectType.Icon ?? "bi-folder",
                ProjectTypeColor = projectType.CssClass ?? "bg-secondary",
                DailyTarget = existing?.DailyTarget,
                WeeklyTarget = existing?.WeeklyTarget,
                MonthlyTarget = existing?.MonthlyTarget,
                YearlyTarget = existing?.YearlyTarget,
                SuccessThreshold = existing?.SuccessThreshold ?? 80,
                WarningThreshold = existing?.WarningThreshold ?? 60,
                IsActive = existing?.IsActive ?? true
            });
        }

        return result.OrderBy(r => r.ProjectTypeId).ToList();
    }

    public async Task<PerformanceSettingsDto?> GetByProjectTypeIdAsync(int projectTypeId)
    {
        var setting = await _context.PerformanceSettings
            .FirstOrDefaultAsync(ps => ps.ProjectTypeId == projectTypeId && !ps.IsDeleted);

        var projectType = ProjectTypes.GetById(projectTypeId);
        if (projectType == null)
            return null;

        // Ayar yoksa varsayılan değerlerle döndür
        if (setting == null)
        {
            return new PerformanceSettingsDto
            {
                Id = 0,
                ProjectTypeId = projectType.Id,
                ProjectTypeName = projectType.Description,
                ProjectTypeIcon = projectType.Icon ?? "bi-folder",
                ProjectTypeColor = projectType.CssClass ?? "bg-secondary",
                SuccessThreshold = 80,
                WarningThreshold = 60,
                IsActive = true
            };
        }

        return MapToDto(setting, projectType);
    }

    public async Task<PerformanceSettingsDto> SaveAsync(SavePerformanceSettingsDto dto, string? userId = null)
    {
        var projectType = ProjectTypes.GetById(dto.ProjectTypeId);
        if (projectType == null)
            throw new ArgumentException($"Invalid project type ID: {dto.ProjectTypeId}");

        var existing = await _context.PerformanceSettings
            .FirstOrDefaultAsync(ps => ps.ProjectTypeId == dto.ProjectTypeId && !ps.IsDeleted);

        if (existing == null)
        {
            // Yeni kayıt oluştur
            existing = new PerformanceSettings
            {
                ProjectTypeId = dto.ProjectTypeId,
                CreatedBy = userId
            };
            _context.PerformanceSettings.Add(existing);
        }
        else
        {
            existing.UpdatedBy = userId;
        }

        // Değerleri güncelle
        existing.DailyTarget = dto.DailyTarget;
        existing.WeeklyTarget = dto.WeeklyTarget;
        existing.MonthlyTarget = dto.MonthlyTarget;
        existing.YearlyTarget = dto.YearlyTarget;
        existing.SuccessThreshold = dto.SuccessThreshold;
        existing.WarningThreshold = dto.WarningThreshold;
        existing.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return MapToDto(existing, projectType);
    }

    public async Task<List<PerformanceSettingsDto>> BulkSaveAsync(BulkSavePerformanceSettingsDto dto, string? userId = null)
    {
        var result = new List<PerformanceSettingsDto>();

        foreach (var settingDto in dto.Settings)
        {
            var saved = await SaveAsync(settingDto, userId);
            result.Add(saved);
        }

        return result;
    }

    public async Task<bool> DeleteAsync(int projectTypeId, string? userId = null)
    {
        var setting = await _context.PerformanceSettings
            .FirstOrDefaultAsync(ps => ps.ProjectTypeId == projectTypeId && !ps.IsDeleted);

        if (setting == null)
            return false;

        setting.IsDeleted = true;
        setting.UpdatedBy = userId;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<Dictionary<int, PerformanceSettingsListDto>> GetActiveSettingsAsync()
    {
        var settings = await _context.PerformanceSettings
            .Where(ps => !ps.IsDeleted && ps.IsActive)
            .ToListAsync();

        var result = new Dictionary<int, PerformanceSettingsListDto>();

        foreach (var setting in settings)
        {
            var projectType = ProjectTypes.GetById(setting.ProjectTypeId);
            if (projectType == null) continue;

            result[setting.ProjectTypeId] = new PerformanceSettingsListDto
            {
                Id = setting.Id,
                ProjectTypeId = setting.ProjectTypeId,
                ProjectTypeName = projectType.Description,
                ProjectTypeIcon = projectType.Icon ?? "bi-folder",
                ProjectTypeColor = projectType.CssClass ?? "bg-secondary",
                DailyTarget = setting.DailyTarget,
                WeeklyTarget = setting.WeeklyTarget,
                MonthlyTarget = setting.MonthlyTarget,
                YearlyTarget = setting.YearlyTarget,
                SuccessThreshold = setting.SuccessThreshold,
                WarningThreshold = setting.WarningThreshold,
                IsActive = setting.IsActive
            };
        }

        return result;
    }

    private static PerformanceSettingsDto MapToDto(PerformanceSettings setting, TypeItem projectType)
    {
        return new PerformanceSettingsDto
        {
            Id = setting.Id,
            ProjectTypeId = setting.ProjectTypeId,
            ProjectTypeName = projectType.Description,
            ProjectTypeIcon = projectType.Icon ?? "bi-folder",
            ProjectTypeColor = projectType.CssClass ?? "bg-secondary",
            DailyTarget = setting.DailyTarget,
            WeeklyTarget = setting.WeeklyTarget,
            MonthlyTarget = setting.MonthlyTarget,
            YearlyTarget = setting.YearlyTarget,
            SuccessThreshold = setting.SuccessThreshold,
            WarningThreshold = setting.WarningThreshold,
            IsActive = setting.IsActive,
            CreatedAt = setting.CreatedAt,
            UpdatedAt = setting.UpdatedAt,
            CreatedBy = setting.CreatedBy,
            UpdatedBy = setting.UpdatedBy
        };
    }
}
