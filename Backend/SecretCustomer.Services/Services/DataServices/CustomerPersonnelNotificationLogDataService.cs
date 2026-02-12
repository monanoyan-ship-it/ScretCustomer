using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class CustomerPersonnelNotificationLogDataService : ICustomerPersonnelNotificationLogDataService
{
    private readonly ApplicationDbContext _context;

    public CustomerPersonnelNotificationLogDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerPersonnelNotificationLog?> GetByIdAsync(int id)
        => await _context.CustomerPersonnelNotificationLogs.FindAsync(id);

    public async Task<List<CustomerPersonnelNotificationLog>> GetAllAsync()
        => await _context.CustomerPersonnelNotificationLogs.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(CustomerPersonnelNotificationLog entity)
    {
        _context.CustomerPersonnelNotificationLogs.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerPersonnelNotificationLog entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.CustomerPersonnelNotificationLogs.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.CustomerPersonnelNotificationLogs.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
