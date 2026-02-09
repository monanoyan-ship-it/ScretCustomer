using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class QuestionDataService : IQuestionDataService
{
    private readonly ApplicationDbContext _context;

    public QuestionDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Question?> GetByIdAsync(int id)
        => await _context.Questions.FindAsync(id);

    public async Task<List<Question>> GetAllAsync()
        => await _context.Questions.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(Question entity)
    {
        _context.Questions.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Question entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Questions.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Questions.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
