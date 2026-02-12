using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class TrainingQuizResponseDataService : ITrainingQuizResponseDataService
{
    private readonly ApplicationDbContext _context;

    public TrainingQuizResponseDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TrainingQuizResponse?> GetByIdAsync(int id)
        => await _context.TrainingQuizResponses.FindAsync(id);

    public async Task<List<TrainingQuizResponse>> GetAllAsync()
        => await _context.TrainingQuizResponses.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(TrainingQuizResponse entity)
    {
        _context.TrainingQuizResponses.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(TrainingQuizResponse entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.TrainingQuizResponses.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.TrainingQuizResponses.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
