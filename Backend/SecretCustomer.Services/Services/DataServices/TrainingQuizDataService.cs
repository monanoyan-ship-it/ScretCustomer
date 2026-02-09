using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class TrainingQuizDataService : ITrainingQuizDataService
{
    private readonly ApplicationDbContext _context;

    public TrainingQuizDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingQuiz?> GetByIdAsync(int id)
        => await _context.TrainingQuizzes.FindAsync(id);

    public async Task<List<TrainingQuiz>> GetAllAsync()
        => await _context.TrainingQuizzes.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(TrainingQuiz entity)
    {
        _context.TrainingQuizzes.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TrainingQuiz entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.TrainingQuizzes.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TrainingQuizzes.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
