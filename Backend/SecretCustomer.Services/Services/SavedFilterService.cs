using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.SavedFilter;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class SavedFilterService : ISavedFilterService
{
    private readonly ApplicationDbContext _context;

    public SavedFilterService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<SavedFilterDto>> GetByUserAndPageAsync(int userId, string pageName)
    {
        return await _context.SavedFilters
            .Where(f => f.UserId == userId && f.PageName == pageName && !f.IsDeleted)
            .OrderByDescending(f => f.IsDefault)
            .ThenByDescending(f => f.CreatedAt)
            .Select(f => MapToDto(f))
            .ToListAsync();
    }

    public async Task<SavedFilterDto?> GetByIdAsync(int id)
    {
        var filter = await _context.SavedFilters
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        return filter != null ? MapToDto(filter) : null;
    }

    public async Task<SavedFilterDto> CreateAsync(int userId, CreateSavedFilterDto dto)
    {
        // Eğer varsayılan olarak işaretlendiyse, diğerlerini kaldır
        if (dto.IsDefault)
        {
            await ClearDefaultAsync(userId, dto.PageName);
        }

        var filter = new SavedFilter
        {
            UserId = userId,
            PageName = dto.PageName,
            Name = dto.Name,
            Description = dto.Description,
            FilterData = dto.FilterData,
            IsDefault = dto.IsDefault
        };

        _context.SavedFilters.Add(filter);
        await _context.SaveChangesAsync();

        return MapToDto(filter);
    }

    public async Task DeleteAsync(int id, int userId)
    {
        var filter = await _context.SavedFilters
            .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId && !f.IsDeleted);

        if (filter == null)
            throw new KeyNotFoundException("Filtre bulunamadı.");

        filter.IsDeleted = true;
        filter.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task SetDefaultAsync(int userId, string pageName, int filterId)
    {
        await ClearDefaultAsync(userId, pageName);

        if (filterId > 0)
        {
            var filter = await _context.SavedFilters
                .FirstOrDefaultAsync(f => f.Id == filterId && f.UserId == userId && !f.IsDeleted);

            if (filter != null)
            {
                filter.IsDefault = true;
                filter.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task ClearDefaultAsync(int userId, string pageName)
    {
        var defaults = await _context.SavedFilters
            .Where(f => f.UserId == userId && f.PageName == pageName && f.IsDefault && !f.IsDeleted)
            .ToListAsync();

        foreach (var f in defaults)
        {
            f.IsDefault = false;
            f.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static SavedFilterDto MapToDto(SavedFilter filter)
    {
        return new SavedFilterDto
        {
            Id = filter.Id,
            UserId = filter.UserId,
            PageName = filter.PageName,
            Name = filter.Name,
            Description = filter.Description,
            FilterData = filter.FilterData,
            IsDefault = filter.IsDefault,
            CreatedAt = filter.CreatedAt
        };
    }
}
