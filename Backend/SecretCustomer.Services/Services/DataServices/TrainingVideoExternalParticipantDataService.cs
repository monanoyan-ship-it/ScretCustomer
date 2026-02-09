using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class TrainingVideoExternalParticipantDataService : ITrainingVideoExternalParticipantDataService
{
    private readonly ApplicationDbContext _context;

    public TrainingVideoExternalParticipantDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingVideoExternalParticipant?> GetByIdAsync(int id)
        => await _context.TrainingVideoExternalParticipants.FindAsync(id);

    public async Task<List<TrainingVideoExternalParticipant>> GetAllAsync()
        => await _context.TrainingVideoExternalParticipants.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(TrainingVideoExternalParticipant entity)
    {
        _context.TrainingVideoExternalParticipants.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TrainingVideoExternalParticipant entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.TrainingVideoExternalParticipants.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TrainingVideoExternalParticipants.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
