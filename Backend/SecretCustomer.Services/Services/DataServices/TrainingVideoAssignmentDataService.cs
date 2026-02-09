using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class TrainingVideoAssignmentDataService : ITrainingVideoAssignmentDataService
{
    private readonly ApplicationDbContext _context;

    public TrainingVideoAssignmentDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingVideoAssignment?> GetByIdAsync(int id)
        => await _context.TrainingVideoAssignments.FindAsync(id);

    public async Task<List<TrainingVideoAssignment>> GetAllAsync()
        => await _context.TrainingVideoAssignments.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(TrainingVideoAssignment entity)
    {
        _context.TrainingVideoAssignments.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TrainingVideoAssignment entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.TrainingVideoAssignments.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TrainingVideoAssignments.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
