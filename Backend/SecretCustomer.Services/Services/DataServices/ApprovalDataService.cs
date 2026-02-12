using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class ApprovalDataService : IApprovalDataService
{
    private readonly ApplicationDbContext _context;

    public ApprovalDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Approval?> GetByIdAsync(int id)
        => await _context.Approvals.FindAsync(id);

    public async Task<List<Approval>> GetAllAsync()
        => await _context.Approvals.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(Approval entity)
    {
        _context.Approvals.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Approval entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.Approvals.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Approvals.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
