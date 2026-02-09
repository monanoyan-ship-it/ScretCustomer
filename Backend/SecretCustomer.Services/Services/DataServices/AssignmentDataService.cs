using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class AssignmentDataService : IAssignmentDataService
{
    private readonly ApplicationDbContext _context;

    public AssignmentDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Assignment?> GetByIdAsync(int id)
        => await _context.Assignments.FindAsync(id);

    public async Task<List<Assignment>> GetAllAsync()
        => await _context.Assignments.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(Assignment entity)
    {
        _context.Assignments.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Assignment entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Assignments.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Assignments.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
