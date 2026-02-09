using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class NotificationDataService : INotificationDataService
{
    private readonly ApplicationDbContext _context;

    public NotificationDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Notification?> GetByIdAsync(int id)
        => await _context.Notifications.FindAsync(id);

    public async Task<List<Notification>> GetAllAsync()
        => await _context.Notifications.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(Notification entity)
    {
        _context.Notifications.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Notification entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Notifications.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Notifications.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
