using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class CustomerPersonnelRepository : ICustomerPersonnelRepository
{
    private readonly ApplicationDbContext _context;

    public CustomerPersonnelRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerPersonnel?> GetByIdAsync(Guid id, bool includeDetails = false)
    {
        var query = _context.CustomerPersonnel.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(p => p.Customer)
                .Include(p => p.TaskAssignments)
                .Include(p => p.Permissions);
        }

        return await query.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<CustomerPersonnel>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.CustomerPersonnel
            .Include(p => p.Customer)
            .Include(p => p.TaskAssignments)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.FirstName)
            .ThenBy(p => p.LastName)
            .ToListAsync();
    }

    public async Task<IEnumerable<CustomerPersonnel>> GetByCustomerIdAsync(Guid customerId, bool includeInactive = false)
    {
        var query = _context.CustomerPersonnel
            .Include(p => p.Customer)
            .Include(p => p.TaskAssignments)
            .Where(p => p.CustomerId == customerId);

        if (!includeInactive)
        {
            query = query.Where(p => p.IsActive);
        }

        return await query
            .OrderBy(p => p.FirstName)
            .ThenBy(p => p.LastName)
            .ToListAsync();
    }

    public async Task<CustomerPersonnel?> GetByUsernameAsync(string username)
    {
        return await _context.CustomerPersonnel
            .Include(p => p.Customer)
            .Include(p => p.Permissions)
            .FirstOrDefaultAsync(p => p.Username == username);
    }

    public async Task<CustomerPersonnel?> GetByEmailAsync(string email)
    {
        return await _context.CustomerPersonnel
            .Include(p => p.Customer)
            .Include(p => p.Permissions)
            .FirstOrDefaultAsync(p => p.Email == email);
    }

    public async Task<CustomerPersonnel> CreateAsync(CustomerPersonnel personnel)
    {
        _context.CustomerPersonnel.Add(personnel);
        await _context.SaveChangesAsync();
        return personnel;
    }

    public async Task<CustomerPersonnel> UpdateAsync(CustomerPersonnel personnel)
    {
        _context.CustomerPersonnel.Update(personnel);
        await _context.SaveChangesAsync();
        return personnel;
    }

    public async Task DeleteAsync(Guid id)
    {
        var personnel = await _context.CustomerPersonnel.FindAsync(id);
        if (personnel != null)
        {
            personnel.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByUsernameAsync(string username, Guid? excludeId = null)
    {
        return await _context.CustomerPersonnel
            .AnyAsync(p => p.Username == username && (excludeId == null || p.Id != excludeId));
    }

    public async Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null)
    {
        return await _context.CustomerPersonnel
            .AnyAsync(p => p.Email == email && (excludeId == null || p.Id != excludeId));
    }
}
