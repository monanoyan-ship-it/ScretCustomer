using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class CustomerOrganizationDataService : ICustomerOrganizationDataService
{
    private readonly ApplicationDbContext _context;

    public CustomerOrganizationDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerOrganization?> GetByIdAsync(int id)
        => await _context.CustomerOrganizations.FindAsync(id);

    public async Task<List<CustomerOrganization>> GetAllAsync()
        => await _context.CustomerOrganizations.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(CustomerOrganization entity)
    {
        _context.CustomerOrganizations.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerOrganization entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.CustomerOrganizations.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.CustomerOrganizations.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
