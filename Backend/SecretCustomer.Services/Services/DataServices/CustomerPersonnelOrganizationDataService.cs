using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class CustomerPersonnelOrganizationDataService : ICustomerPersonnelOrganizationDataService
{
    private readonly ApplicationDbContext _context;

    public CustomerPersonnelOrganizationDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerPersonnelOrganization?> GetByIdAsync(int id)
        => await _context.CustomerPersonnelOrganizations.FindAsync(id);

    public async Task<List<CustomerPersonnelOrganization>> GetAllAsync()
        => await _context.CustomerPersonnelOrganizations.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(CustomerPersonnelOrganization entity)
    {
        _context.CustomerPersonnelOrganizations.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerPersonnelOrganization entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.CustomerPersonnelOrganizations.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.CustomerPersonnelOrganizations.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
