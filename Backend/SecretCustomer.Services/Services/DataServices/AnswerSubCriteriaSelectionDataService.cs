using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class AnswerSubCriteriaSelectionDataService : IAnswerSubCriteriaSelectionDataService
{
    private readonly ApplicationDbContext _context;

    public AnswerSubCriteriaSelectionDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AnswerSubCriteriaSelection?> GetByIdAsync(int id)
        => await _context.AnswerSubCriteriaSelections.FindAsync(id);

    public async Task<List<AnswerSubCriteriaSelection>> GetAllAsync()
        => await _context.AnswerSubCriteriaSelections.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(AnswerSubCriteriaSelection entity)
    {
        _context.AnswerSubCriteriaSelections.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AnswerSubCriteriaSelection entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.AnswerSubCriteriaSelections.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.AnswerSubCriteriaSelections.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
