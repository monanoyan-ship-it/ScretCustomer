using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class TrainingVideoExternalEmailLogDataService : ITrainingVideoExternalEmailLogDataService
{
    private readonly ApplicationDbContext _context;

    public TrainingVideoExternalEmailLogDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingVideoExternalEmailLog?> GetByIdAsync(int id)
        => await _context.TrainingVideoExternalEmailLogs.FindAsync(id);

    public async Task<List<TrainingVideoExternalEmailLog>> GetAllAsync()
        => await _context.TrainingVideoExternalEmailLogs.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(TrainingVideoExternalEmailLog entity)
    {
        _context.TrainingVideoExternalEmailLogs.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TrainingVideoExternalEmailLog entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.TrainingVideoExternalEmailLogs.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TrainingVideoExternalEmailLogs.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
