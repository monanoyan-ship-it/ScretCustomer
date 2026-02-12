using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class CustomerPersonnelPermissionDataService : ICustomerPersonnelPermissionDataService
{
    private readonly ApplicationDbContext _context;

    public CustomerPersonnelPermissionDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerPersonnelPermission?> GetByIdAsync(int id)
        => await _context.CustomerPersonnelPermissions.FindAsync(id);

    public async Task<List<CustomerPersonnelPermission>> GetAllAsync()
        => await _context.CustomerPersonnelPermissions.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(CustomerPersonnelPermission entity)
    {
        _context.CustomerPersonnelPermissions.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerPersonnelPermission entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.CustomerPersonnelPermissions.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.CustomerPersonnelPermissions.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
