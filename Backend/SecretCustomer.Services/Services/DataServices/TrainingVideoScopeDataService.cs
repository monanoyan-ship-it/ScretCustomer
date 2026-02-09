using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class TrainingVideoScopeDataService : ITrainingVideoScopeDataService
{
    private readonly ApplicationDbContext _context;

    public TrainingVideoScopeDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingVideoScope?> GetByIdAsync(int id)
        => await _context.TrainingVideoScopes.FindAsync(id);

    public async Task<List<TrainingVideoScope>> GetAllAsync()
        => await _context.TrainingVideoScopes.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(TrainingVideoScope entity)
    {
        _context.TrainingVideoScopes.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TrainingVideoScope entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.TrainingVideoScopes.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TrainingVideoScopes.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
