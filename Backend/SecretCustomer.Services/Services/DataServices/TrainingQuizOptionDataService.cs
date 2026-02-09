using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class TrainingQuizOptionDataService : ITrainingQuizOptionDataService
{
    private readonly ApplicationDbContext _context;

    public TrainingQuizOptionDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingQuizOption?> GetByIdAsync(int id)
        => await _context.TrainingQuizOptions.FindAsync(id);

    public async Task<List<TrainingQuizOption>> GetAllAsync()
        => await _context.TrainingQuizOptions.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(TrainingQuizOption entity)
    {
        _context.TrainingQuizOptions.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TrainingQuizOption entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.TrainingQuizOptions.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TrainingQuizOptions.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
