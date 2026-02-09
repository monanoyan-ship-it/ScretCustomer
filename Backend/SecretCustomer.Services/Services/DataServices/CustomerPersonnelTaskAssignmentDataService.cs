using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class CustomerPersonnelTaskAssignmentDataService : ICustomerPersonnelTaskAssignmentDataService
{
    private readonly ApplicationDbContext _context;

    public CustomerPersonnelTaskAssignmentDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CustomerPersonnelTaskAssignment?> GetByIdAsync(int id)
        => await _context.CustomerPersonnelTaskAssignments.FindAsync(id);

    public async Task<List<CustomerPersonnelTaskAssignment>> GetAllAsync()
        => await _context.CustomerPersonnelTaskAssignments.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(CustomerPersonnelTaskAssignment entity)
    {
        _context.CustomerPersonnelTaskAssignments.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(CustomerPersonnelTaskAssignment entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.CustomerPersonnelTaskAssignments.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.CustomerPersonnelTaskAssignments.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
