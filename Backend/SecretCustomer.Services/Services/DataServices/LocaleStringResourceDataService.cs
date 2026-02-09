using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class LocaleStringResourceDataService : ILocaleStringResourceDataService
{
    private readonly ApplicationDbContext _context;

    public LocaleStringResourceDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LocaleStringResource?> GetByIdAsync(int id)
        => await _context.LocaleStringResources.FindAsync(id);

    public async Task<List<LocaleStringResource>> GetAllAsync()
        => await _context.LocaleStringResources.ToListAsync();

    public async Task AddAsync(LocaleStringResource entity)
    {
        _context.LocaleStringResources.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(LocaleStringResource entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.LocaleStringResources.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.LocaleStringResources.FindAsync(id);
        if (entity != null)
        {
            _context.LocaleStringResources.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
