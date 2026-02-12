using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class QuestionAttachmentDataService : IQuestionAttachmentDataService
{
    private readonly ApplicationDbContext _context;

    public QuestionAttachmentDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<QuestionAttachment?> GetByIdAsync(int id)
        => await _context.QuestionAttachments.FindAsync(id);

    public async Task<List<QuestionAttachment>> GetAllAsync()
        => await _context.QuestionAttachments.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(QuestionAttachment entity)
    {
        _context.QuestionAttachments.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(QuestionAttachment entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.QuestionAttachments.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.QuestionAttachments.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
