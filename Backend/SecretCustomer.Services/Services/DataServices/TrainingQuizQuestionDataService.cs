using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class TrainingQuizQuestionDataService : ITrainingQuizQuestionDataService
{
    private readonly ApplicationDbContext _context;

    public TrainingQuizQuestionDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingQuizQuestion?> GetByIdAsync(int id)
        => await _context.TrainingQuizQuestions.FindAsync(id);

    public async Task<List<TrainingQuizQuestion>> GetAllAsync()
        => await _context.TrainingQuizQuestions.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(TrainingQuizQuestion entity)
    {
        _context.TrainingQuizQuestions.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TrainingQuizQuestion entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.TrainingQuizQuestions.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TrainingQuizQuestions.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
