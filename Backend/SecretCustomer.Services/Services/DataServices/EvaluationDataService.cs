using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class EvaluationDataService : IEvaluationDataService
{
    private readonly ApplicationDbContext _context;

    public EvaluationDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Evaluation?> GetByIdAsync(int id)
        => await _context.Evaluations.FindAsync(id);

    public async Task<List<Evaluation>> GetAllAsync()
        => await _context.Evaluations.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(Evaluation entity)
    {
        _context.Evaluations.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Evaluation entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.Evaluations.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Evaluations.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
