using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class AppSettingsDataService : IAppSettingsDataService
{
    private readonly ApplicationDbContext _context;

    public AppSettingsDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AppSettings?> GetByIdAsync(int id)
        => await _context.AppSettings.FindAsync(id);

    public async Task<List<AppSettings>> GetAllAsync()
        => await _context.AppSettings.ToListAsync();

    public async Task AddAsync(AppSettings entity)
    {
        _context.AppSettings.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AppSettings entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.AppSettings.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.AppSettings.FindAsync(id);
        if (entity != null)
        {
            _context.AppSettings.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
