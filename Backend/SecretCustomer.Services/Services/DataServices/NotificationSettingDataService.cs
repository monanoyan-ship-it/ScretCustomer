using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class NotificationSettingDataService : INotificationSettingDataService
{
    private readonly ApplicationDbContext _context;

    public NotificationSettingDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NotificationSetting?> GetByIdAsync(int id)
        => await _context.NotificationSettings.FindAsync(id);

    public async Task<List<NotificationSetting>> GetAllAsync()
        => await _context.NotificationSettings.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(NotificationSetting entity)
    {
        _context.NotificationSettings.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(NotificationSetting entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.NotificationSettings.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.NotificationSettings.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
