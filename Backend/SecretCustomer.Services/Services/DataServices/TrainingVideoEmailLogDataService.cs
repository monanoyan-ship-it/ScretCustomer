using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class TrainingVideoEmailLogDataService : ITrainingVideoEmailLogDataService
{
    private readonly ApplicationDbContext _context;

    public TrainingVideoEmailLogDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingVideoEmailLog?> GetByIdAsync(int id)
        => await _context.TrainingVideoEmailLogs.FindAsync(id);

    public async Task<List<TrainingVideoEmailLog>> GetAllAsync()
        => await _context.TrainingVideoEmailLogs.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(TrainingVideoEmailLog entity)
    {
        _context.TrainingVideoEmailLogs.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TrainingVideoEmailLog entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.TrainingVideoEmailLogs.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TrainingVideoEmailLogs.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
