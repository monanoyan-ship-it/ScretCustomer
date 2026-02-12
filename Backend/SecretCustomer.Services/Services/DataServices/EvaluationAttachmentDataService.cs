using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class EvaluationAttachmentDataService : IEvaluationAttachmentDataService
{
    private readonly ApplicationDbContext _context;

    public EvaluationAttachmentDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EvaluationAttachment?> GetByIdAsync(int id)
        => await _context.EvaluationAttachments.FindAsync(id);

    public async Task<List<EvaluationAttachment>> GetAllAsync()
        => await _context.EvaluationAttachments.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(EvaluationAttachment entity)
    {
        _context.EvaluationAttachments.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(EvaluationAttachment entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.EvaluationAttachments.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.EvaluationAttachments.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
