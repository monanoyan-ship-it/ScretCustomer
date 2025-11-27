using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class BranchRepository : IBranchRepository
{
    private readonly ApplicationDbContext _context;

    public BranchRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Branch?> GetByIdAsync(Guid id, bool includeDetails = false)
    {
        var query = _context.Branches.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(b => b.Users)
                .Include(b => b.Assignments);
        }

        return await query.FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Branch>> GetAllAsync(bool includeInactive = false)
    {
        var query = _context.Branches
            .Include(b => b.Users)
            .Include(b => b.Assignments)
            .AsQueryable();

        if (!includeInactive)
        {
            query = query.Where(b => b.IsActive);
        }

        return await query
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Branch>> GetActiveAsync()
    {
        return await _context.Branches
            .Include(b => b.Users)
            .Include(b => b.Assignments)
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<Branch?> GetByCodeAsync(string code)
    {
        return await _context.Branches
            .Include(b => b.Users)
            .Include(b => b.Assignments)
            .FirstOrDefaultAsync(b => b.Code == code);
    }

    public async Task<IEnumerable<Branch>> GetByRegionAsync(string region)
    {
        return await _context.Branches
            .Include(b => b.Users)
            .Include(b => b.Assignments)
            .Where(b => b.Region == region)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<IEnumerable<Branch>> GetByCityAsync(string city)
    {
        return await _context.Branches
            .Include(b => b.Users)
            .Include(b => b.Assignments)
            .Where(b => b.City == city)
            .OrderBy(b => b.Name)
            .ToListAsync();
    }

    public async Task<Branch> CreateAsync(Branch branch)
    {
        _context.Branches.Add(branch);
        await _context.SaveChangesAsync();
        return branch;
    }

    public async Task<Branch> UpdateAsync(Branch branch)
    {
        branch.UpdatedAt = DateTime.UtcNow;
        _context.Branches.Update(branch);
        await _context.SaveChangesAsync();
        return branch;
    }

    public async Task DeleteAsync(Guid id)
    {
        var branch = await _context.Branches.FindAsync(id);
        if (branch != null)
        {
            branch.IsDeleted = true;
            branch.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<bool> ExistsByCodeAsync(string code, Guid? excludeId = null)
    {
        var query = _context.Branches.Where(b => b.Code == code);

        if (excludeId.HasValue)
        {
            query = query.Where(b => b.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }
}
