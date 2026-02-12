using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class SystemSettingDataService : ISystemSettingDataService
{
    private readonly ApplicationDbContext _context;

    public SystemSettingDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SystemSetting?> GetByIdAsync(int id)
        => await _context.SystemSettings.FindAsync(id);

    public async Task<List<SystemSetting>> GetAllAsync()
        => await _context.SystemSettings.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(SystemSetting entity)
    {
        _context.SystemSettings.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SystemSetting entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.SystemSettings.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.SystemSettings.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
