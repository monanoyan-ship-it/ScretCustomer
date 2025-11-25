using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class ChecklistRepository : IChecklistRepository
{
    private readonly ApplicationDbContext _context;

    public ChecklistRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Checklist?> GetByIdAsync(Guid id, bool includeDetails = false)
    {
        var query = _context.Checklists
            .Where(c => !c.IsDeleted)
            .AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(c => c.Sections.Where(s => !s.IsDeleted).OrderBy(s => s.Order))
                    .ThenInclude(s => s.Questions.Where(q => !q.IsDeleted).OrderBy(q => q.Order));
        }

        return await query.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Checklist>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Checklists
            .Where(c => !c.IsDeleted)
            .Include(c => c.Sections.Where(s => !s.IsDeleted))
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<Checklist> CreateAsync(Checklist checklist)
    {
        _context.Checklists.Add(checklist);
        await _context.SaveChangesAsync();
        return checklist;
    }

    public async Task<Checklist> UpdateAsync(Checklist checklist)
    {
        _context.Checklists.Update(checklist);
        await _context.SaveChangesAsync();
        return checklist;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var checklist = await _context.Checklists.FindAsync(id);
        if (checklist == null) return false;

        checklist.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(Guid id)
    {
        return await _context.Checklists.AnyAsync(c => c.Id == id);
    }

    public async Task<int> GetVersionCountAsync(string name)
    {
        return await _context.Checklists.CountAsync(c => c.Name.StartsWith(name));
    }
}
