using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class PerformanceSettingsDataService : IPerformanceSettingsDataService
{
    private readonly ApplicationDbContext _context;

    public PerformanceSettingsDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PerformanceSettings?> GetByIdAsync(int id)
        => await _context.PerformanceSettings.FindAsync(id);

    public async Task<List<PerformanceSettings>> GetAllAsync()
        => await _context.PerformanceSettings.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(PerformanceSettings entity)
    {
        _context.PerformanceSettings.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(PerformanceSettings entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.PerformanceSettings.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.PerformanceSettings.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
