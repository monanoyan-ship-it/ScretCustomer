using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class CustomerNotificationRuleDataService : ICustomerNotificationRuleDataService
{
    private readonly ApplicationDbContext _context;

    public CustomerNotificationRuleDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerNotificationRule?> GetByIdAsync(int id)
        => await _context.CustomerNotificationRules.FindAsync(id);

    public async Task<List<CustomerNotificationRule>> GetAllAsync()
        => await _context.CustomerNotificationRules.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(CustomerNotificationRule entity)
    {
        _context.CustomerNotificationRules.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerNotificationRule entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.CustomerNotificationRules.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.CustomerNotificationRules.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
