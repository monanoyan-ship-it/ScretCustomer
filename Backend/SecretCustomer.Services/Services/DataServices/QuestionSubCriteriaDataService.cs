using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class QuestionSubCriteriaDataService : IQuestionSubCriteriaDataService
{
    private readonly ApplicationDbContext _context;

    public QuestionSubCriteriaDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<QuestionSubCriteria?> GetByIdAsync(int id)
        => await _context.QuestionSubCriteria.FindAsync(id);

    public async Task<List<QuestionSubCriteria>> GetAllAsync()
        => await _context.QuestionSubCriteria.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(QuestionSubCriteria entity)
    {
        _context.QuestionSubCriteria.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(QuestionSubCriteria entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.QuestionSubCriteria.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.QuestionSubCriteria.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
