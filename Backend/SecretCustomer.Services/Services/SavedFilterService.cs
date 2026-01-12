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

    /// <summary>
    /// Admin panel için - tüm adminlerin filtrelerini getir (CustomerId null olanlar)
    /// </summary>
    public async Task<IEnumerable<SavedFilterDto>> GetByUserAndPageAsync(int userId, string pageName)
    {
        // Admin filtreleri: CustomerId null olanlar - tüm adminler görür
        return await _context.SavedFilters
            .Where(f => f.CustomerId == null && f.PageName == pageName && !f.IsDeleted)
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

    /// <summary>
    /// Admin panel için filtre kaydet - CustomerId null, UserId kim kaydetti bilgisi
    /// </summary>
    public async Task<SavedFilterDto> CreateAsync(int userId, CreateSavedFilterDto dto)
    {
        // Eğer varsayılan olarak işaretlendiyse, diğerlerini kaldır
        if (dto.IsDefault)
        {
            await ClearDefaultForAdminAsync(dto.PageName);
        }

        var filter = new SavedFilter
        {
            UserId = userId, // Kim kaydetti bilgisi
            CustomerId = null, // Admin filtresi
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

    /// <summary>
    /// Admin filtresi sil - herhangi bir admin silebilir
    /// </summary>
    public async Task DeleteAsync(int id, int userId)
    {
        var filter = await _context.SavedFilters
            .FirstOrDefaultAsync(f => f.Id == id && f.CustomerId == null && !f.IsDeleted);

        if (filter == null)
            throw new KeyNotFoundException("Filtre bulunamadı.");

        filter.IsDeleted = true;
        filter.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task SetDefaultAsync(int userId, string pageName, int filterId)
    {
        await ClearDefaultForAdminAsync(pageName);

        if (filterId > 0)
        {
            var filter = await _context.SavedFilters
                .FirstOrDefaultAsync(f => f.Id == filterId && f.CustomerId == null && !f.IsDeleted);

            if (filter != null)
            {
                filter.IsDefault = true;
                filter.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Admin filtreleri için varsayılanları temizle (tüm adminler için)
    /// </summary>
    private async Task ClearDefaultForAdminAsync(string pageName)
    {
        var defaults = await _context.SavedFilters
            .Where(f => f.CustomerId == null && f.PageName == pageName && f.IsDefault && !f.IsDeleted)
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
            UserId = filter.UserId ?? 0,
            PageName = filter.PageName,
            Name = filter.Name,
            Description = filter.Description,
            FilterData = filter.FilterData,
            IsDefault = filter.IsDefault,
            CreatedAt = filter.CreatedAt
        };
    }
}
