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

    public async Task<Checklist?> GetByIdAsync(int id, bool includeDetails = false)
    {
        var query = _context.Checklists
            .Where(c => !c.IsDeleted)
            .Include(c => c.Customer)
            .Include(c => c.CustomerOrganization)
            .AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(c => c.Questions.Where(q => !q.IsDeleted).OrderBy(q => q.Order))
                    .ThenInclude(q => q.SubCriteria.Where(sc => !sc.IsDeleted).OrderBy(sc => sc.Order));
        }

        return await query.FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<IEnumerable<Checklist>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Checklists
            .Where(c => !c.IsDeleted)
            .Include(c => c.Questions.Where(q => !q.IsDeleted))
            .Include(c => c.Customer)
            .Include(c => c.CustomerOrganization)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderByDescending(c => c.CreatedAt).ToListAsync();
    }

    public async Task<IEnumerable<Checklist>> GetFilteredAsync(string? searchText = null, int? customerId = null, int? customerOrganizationId = null, bool includeInactive = false)
    {
        var query = _context.Checklists
            .Where(c => !c.IsDeleted)
            .Include(c => c.Questions.Where(q => !q.IsDeleted))
            .Include(c => c.Customer)
            .Include(c => c.CustomerOrganization)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(c => c.IsActive);
        }

        // Metin araması (isim veya açıklama)
        if (!string.IsNullOrWhiteSpace(searchText))
        {
            var search = searchText.ToLower();
            query = query.Where(c =>
                c.Name.ToLower().Contains(search) ||
                (c.Description != null && c.Description.ToLower().Contains(search)) ||
                (c.Code != null && c.Code.ToLower().Contains(search)));
        }

        // Firma filtresi
        if (customerId.HasValue)
        {
            query = query.Where(c => c.CustomerId == customerId.Value);
        }

        // Organizasyon filtresi
        if (customerOrganizationId.HasValue)
        {
            query = query.Where(c => c.CustomerOrganizationId == customerOrganizationId.Value);
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
        // Entity zaten tracked - EF Core değişiklikleri otomatik algılar
        await _context.SaveChangesAsync();
        return checklist;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var checklist = await _context.Checklists.FindAsync(id);
        if (checklist == null) return false;

        checklist.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsAsync(int id)
    {
        return await _context.Checklists.AnyAsync(c => c.Id == id && !c.IsDeleted);
    }

    public async Task<int> GetVersionCountAsync(string name)
    {
        return await _context.Checklists.CountAsync(c => c.Name.StartsWith(name) && !c.IsDeleted);
    }
}
