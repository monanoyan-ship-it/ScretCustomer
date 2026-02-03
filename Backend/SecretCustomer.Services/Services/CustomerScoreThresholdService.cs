using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.CustomerScoreThreshold;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class CustomerScoreThresholdService : ICustomerScoreThresholdService
{
    private readonly ApplicationDbContext _context;
    private readonly IPerformanceSettingsService _performanceSettingsService;

    public CustomerScoreThresholdService(ApplicationDbContext context, IPerformanceSettingsService performanceSettingsService)
    {
        _context = context;
        _performanceSettingsService = performanceSettingsService;
    }

    public async Task<List<CustomerScoreThresholdDto>> GetAllAsync(int customerId)
    {
        var existingSettings = await _context.CustomerScoreThresholds
            .Where(s => s.CustomerId == customerId && !s.IsDeleted)
            .ToListAsync();

        // Global performans ayarlarını al (fallback için)
        var globalSettings = await _performanceSettingsService.GetAllAsync();

        var result = new List<CustomerScoreThresholdDto>();

        foreach (var projectType in ProjectTypes.All)
        {
            var existing = existingSettings.FirstOrDefault(s => s.ProjectTypeId == projectType.Id);
            var global = globalSettings.FirstOrDefault(s => s.ProjectTypeId == projectType.Id);

            result.Add(new CustomerScoreThresholdDto
            {
                Id = existing?.Id ?? 0,
                ProjectTypeId = projectType.Id,
                ProjectTypeName = projectType.Description,
                ProjectTypeIcon = projectType.Icon ?? "bi-folder",
                ProjectTypeColor = projectType.CssClass ?? "bg-secondary",
                SuccessThreshold = existing?.SuccessThreshold ?? global?.SuccessThreshold ?? 80,
                WarningThreshold = existing?.WarningThreshold ?? global?.WarningThreshold ?? 60,
                IsActive = existing?.IsActive ?? true
            });
        }

        return result.OrderBy(r => r.ProjectTypeId).ToList();
    }

    public async Task<CustomerScoreThresholdDto> SaveAsync(int customerId, SaveCustomerScoreThresholdDto dto, string? userId = null)
    {
        var projectType = ProjectTypes.GetById(dto.ProjectTypeId);
        if (projectType == null)
            throw new ArgumentException($"Invalid project type ID: {dto.ProjectTypeId}");

        var existing = await _context.CustomerScoreThresholds
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.ProjectTypeId == dto.ProjectTypeId && !s.IsDeleted);

        if (existing == null)
        {
            existing = new CustomerScoreThreshold
            {
                CustomerId = customerId,
                ProjectTypeId = dto.ProjectTypeId,
                CreatedBy = userId
            };
            _context.CustomerScoreThresholds.Add(existing);
        }
        else
        {
            existing.UpdatedBy = userId;
        }

        existing.SuccessThreshold = dto.SuccessThreshold;
        existing.WarningThreshold = dto.WarningThreshold;
        existing.IsActive = dto.IsActive;

        await _context.SaveChangesAsync();

        return new CustomerScoreThresholdDto
        {
            Id = existing.Id,
            ProjectTypeId = existing.ProjectTypeId,
            ProjectTypeName = projectType.Description,
            ProjectTypeIcon = projectType.Icon ?? "bi-folder",
            ProjectTypeColor = projectType.CssClass ?? "bg-secondary",
            SuccessThreshold = existing.SuccessThreshold,
            WarningThreshold = existing.WarningThreshold,
            IsActive = existing.IsActive
        };
    }

    public async Task<List<CustomerScoreThresholdDto>> BulkSaveAsync(int customerId, BulkSaveCustomerScoreThresholdDto dto, string? userId = null)
    {
        var result = new List<CustomerScoreThresholdDto>();

        foreach (var settingDto in dto.Settings)
        {
            var saved = await SaveAsync(customerId, settingDto, userId);
            result.Add(saved);
        }

        return result;
    }

    public async Task<CustomerScoreThresholdDto?> GetByCustomerAndProjectTypeAsync(int customerId, int projectTypeId)
    {
        var setting = await _context.CustomerScoreThresholds
            .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.ProjectTypeId == projectTypeId && !s.IsDeleted && s.IsActive);

        if (setting == null)
            return null;

        var projectType = ProjectTypes.GetById(projectTypeId);
        if (projectType == null)
            return null;

        return new CustomerScoreThresholdDto
        {
            Id = setting.Id,
            ProjectTypeId = setting.ProjectTypeId,
            ProjectTypeName = projectType.Description,
            ProjectTypeIcon = projectType.Icon ?? "bi-folder",
            ProjectTypeColor = projectType.CssClass ?? "bg-secondary",
            SuccessThreshold = setting.SuccessThreshold,
            WarningThreshold = setting.WarningThreshold,
            IsActive = setting.IsActive
        };
    }
}
