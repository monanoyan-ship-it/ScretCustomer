using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class AnswerDataService : IAnswerDataService
{
    private readonly ApplicationDbContext _context;

    public AnswerDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Answer?> GetByIdAsync(int id)
        => await _context.Answers.FindAsync(id);

    public async Task<List<Answer>> GetAllAsync()
        => await _context.Answers.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(Answer entity)
    {
        _context.Answers.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Answer entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Answers.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Answers.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
