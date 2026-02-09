using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class SupportRequestDataService : ISupportRequestDataService
{
    private readonly ApplicationDbContext _context;

    public SupportRequestDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SupportRequest?> GetByIdAsync(int id)
        => await _context.SupportRequests.FindAsync(id);

    public async Task<List<SupportRequest>> GetAllAsync()
        => await _context.SupportRequests.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(SupportRequest entity)
    {
        _context.SupportRequests.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SupportRequest entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.SupportRequests.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.SupportRequests.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
