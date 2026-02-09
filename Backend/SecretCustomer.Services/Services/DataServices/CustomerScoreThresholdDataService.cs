using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class CustomerScoreThresholdDataService : ICustomerScoreThresholdDataService
{
    private readonly ApplicationDbContext _context;

    public CustomerScoreThresholdDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerScoreThreshold?> GetByIdAsync(int id)
        => await _context.CustomerScoreThresholds.FindAsync(id);

    public async Task<List<CustomerScoreThreshold>> GetAllAsync()
        => await _context.CustomerScoreThresholds.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(CustomerScoreThreshold entity)
    {
        _context.CustomerScoreThresholds.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerScoreThreshold entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.CustomerScoreThresholds.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.CustomerScoreThresholds.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
