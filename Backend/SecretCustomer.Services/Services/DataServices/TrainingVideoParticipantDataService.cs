using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class TrainingVideoParticipantDataService : ITrainingVideoParticipantDataService
{
    private readonly ApplicationDbContext _context;

    public TrainingVideoParticipantDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingVideoParticipant?> GetByIdAsync(int id)
        => await _context.TrainingVideoParticipants.FindAsync(id);

    public async Task<List<TrainingVideoParticipant>> GetAllAsync()
        => await _context.TrainingVideoParticipants.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(TrainingVideoParticipant entity)
    {
        _context.TrainingVideoParticipants.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TrainingVideoParticipant entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.TrainingVideoParticipants.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TrainingVideoParticipants.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
