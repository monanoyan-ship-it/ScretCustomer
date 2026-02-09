using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class SavedFilterDataService : ISavedFilterDataService
{
    private readonly ApplicationDbContext _context;

    public SavedFilterDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SavedFilter?> GetByIdAsync(int id)
        => await _context.SavedFilters.FindAsync(id);

    public async Task<List<SavedFilter>> GetAllAsync()
        => await _context.SavedFilters.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(SavedFilter entity)
    {
        _context.SavedFilters.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SavedFilter entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.SavedFilters.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.SavedFilters.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
