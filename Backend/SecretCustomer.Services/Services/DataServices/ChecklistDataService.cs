using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class ChecklistDataService : IChecklistDataService
{
    private readonly ApplicationDbContext _context;

    public ChecklistDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Checklist?> GetByIdAsync(int id)
        => await _context.Checklists.FindAsync(id);

    public async Task<List<Checklist>> GetAllAsync()
        => await _context.Checklists.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(Checklist entity)
    {
        _context.Checklists.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Checklist entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Checklists.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Checklists.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
