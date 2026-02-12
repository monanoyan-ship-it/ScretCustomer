using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class TrainingQuizAnswerDataService : ITrainingQuizAnswerDataService
{
    private readonly ApplicationDbContext _context;

    public TrainingQuizAnswerDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingQuizAnswer?> GetByIdAsync(int id)
        => await _context.TrainingQuizAnswers.FindAsync(id);

    public async Task<List<TrainingQuizAnswer>> GetAllAsync()
        => await _context.TrainingQuizAnswers.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(TrainingQuizAnswer entity)
    {
        _context.TrainingQuizAnswers.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TrainingQuizAnswer entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.TrainingQuizAnswers.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TrainingQuizAnswers.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
