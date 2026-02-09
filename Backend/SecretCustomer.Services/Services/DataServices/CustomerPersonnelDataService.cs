using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class CustomerPersonnelDataService : ICustomerPersonnelDataService
{
    private readonly ApplicationDbContext _context;

    public CustomerPersonnelDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerPersonnel?> GetByIdAsync(int id)
        => await _context.CustomerPersonnel.FindAsync(id);

    public async Task<List<CustomerPersonnel>> GetAllAsync()
        => await _context.CustomerPersonnel.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(CustomerPersonnel entity)
    {
        _context.CustomerPersonnel.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerPersonnel entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.CustomerPersonnel.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.CustomerPersonnel.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
