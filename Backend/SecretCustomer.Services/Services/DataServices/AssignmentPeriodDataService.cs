using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class AssignmentPeriodDataService : IAssignmentPeriodDataService
{
    private readonly ApplicationDbContext _context;

    public AssignmentPeriodDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AssignmentPeriod?> GetByIdAsync(int id)
        => await _context.AssignmentPeriods.FindAsync(id);

    public async Task<List<AssignmentPeriod>> GetAllAsync()
        => await _context.AssignmentPeriods.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(AssignmentPeriod entity)
    {
        _context.AssignmentPeriods.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AssignmentPeriod entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.AssignmentPeriods.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.AssignmentPeriods.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
