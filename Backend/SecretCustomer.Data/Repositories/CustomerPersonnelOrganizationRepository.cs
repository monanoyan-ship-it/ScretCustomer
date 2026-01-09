using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class CustomerPersonnelOrganizationRepository : ICustomerPersonnelOrganizationRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerPersonnelOrganizationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerPersonnelOrganization?> GetByIdAsync(int id)
    {
        return await _context.CustomerPersonnelOrganizations
            .Include(po => po.CustomerPersonnel)
            .Include(po => po.CustomerOrganization)
            .Include(po => po.Supervisor)
            .FirstOrDefaultAsync(po => po.Id == id);
    }

    public async Task<CustomerPersonnelOrganization?> GetAsync(int personnelId, int organizationId)
    {
        return await _context.CustomerPersonnelOrganizations
            .Include(po => po.CustomerPersonnel)
            .Include(po => po.CustomerOrganization)
            .Include(po => po.Supervisor)
            .FirstOrDefaultAsync(po => po.CustomerPersonnelId == personnelId && po.CustomerOrganizationId == organizationId);
    }

    public async Task<IEnumerable<CustomerPersonnelOrganization>> GetByPersonnelIdAsync(int personnelId)
    {
        return await _context.CustomerPersonnelOrganizations
            .Include(po => po.CustomerOrganization)
            .Include(po => po.Supervisor)
            .Where(po => po.CustomerPersonnelId == personnelId)
            .OrderBy(po => po.CustomerOrganization.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<CustomerPersonnelOrganization>> GetByOrganizationIdAsync(int organizationId)
    {
        return await _context.CustomerPersonnelOrganizations
            .Include(po => po.CustomerPersonnel)
            .Include(po => po.Supervisor)
            .Where(po => po.CustomerOrganizationId == organizationId)
            .OrderBy(po => po.CustomerPersonnel.FirstName)
            .ThenBy(po => po.CustomerPersonnel.LastName)
            .ToListAsync();
    }

    public async Task<CustomerPersonnelOrganization> AddAsync(CustomerPersonnelOrganization assignment)
    {
        await _context.CustomerPersonnelOrganizations.AddAsync(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task UpdateAsync(CustomerPersonnelOrganization assignment)
    {
        _context.CustomerPersonnelOrganizations.Update(assignment);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var assignment = await _context.CustomerPersonnelOrganizations.FindAsync(id);
        if (assignment != null)
        {
            assignment.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteAsync(int personnelId, int organizationId)
    {
        var assignment = await _context.CustomerPersonnelOrganizations
            .FirstOrDefaultAsync(po => po.CustomerPersonnelId == personnelId && po.CustomerOrganizationId == organizationId);

        if (assignment != null)
        {
            assignment.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsAsync(int personnelId, int organizationId)
    {
        return await _context.CustomerPersonnelOrganizations
            .AnyAsync(po => po.CustomerPersonnelId == personnelId && po.CustomerOrganizationId == organizationId);
    }
}
