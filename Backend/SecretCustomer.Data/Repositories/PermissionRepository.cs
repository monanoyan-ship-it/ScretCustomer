using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class PermissionRepository : IPermissionRepository
{
    private readonly ApplicationDbContext _context;

    public PermissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByIdAsync(int id)
    {
        return await _context.Permissions
            .Include(p => p.RolePermissions)
            .Include(p => p.UserPermissions)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Permission?> GetByCodeAsync(string code)
    {
        return await _context.Permissions
            .FirstOrDefaultAsync(p => p.Code == code);
    }

    public async Task<IEnumerable<Permission>> GetAllAsync()
    {
        return await _context.Permissions
            .OrderBy(p => p.CategoryId)
            .ThenBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<IEnumerable<Permission>> GetByCategoryAsync(int categoryId)
    {
        return await _context.Permissions
            .Where(p => p.CategoryId == categoryId)
            .OrderBy(p => p.SortOrder)
            .ToListAsync();
    }

    public async Task<Permission> AddAsync(Permission permission)
    {
        await _context.Permissions.AddAsync(permission);
        await _context.SaveChangesAsync();
        return permission;
    }

    public async Task UpdateAsync(Permission permission)
    {
        _context.Permissions.Update(permission);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var permission = await _context.Permissions.FindAsync(id);
        if (permission != null)
        {
            permission.IsDeleted = true;
            await _context.SaveChangesAsync();
        }
    }
}
