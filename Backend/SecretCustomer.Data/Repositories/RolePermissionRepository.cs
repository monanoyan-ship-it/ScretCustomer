using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly ApplicationDbContext _context;

    public RolePermissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RolePermission?> GetByIdAsync(Guid id)
    {
        return await _context.RolePermissions
            .Include(rp => rp.Permission)
            .FirstOrDefaultAsync(rp => rp.Id == id);
    }

    public async Task<IEnumerable<RolePermission>> GetByRoleAsync(UserRole role)
    {
        return await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => rp.Role == role)
            .ToListAsync();
    }

    public async Task<RolePermission?> GetByRoleAndPermissionAsync(UserRole role, Guid permissionId)
    {
        return await _context.RolePermissions
            .Include(rp => rp.Permission)
            .FirstOrDefaultAsync(rp => rp.Role == role && rp.PermissionId == permissionId);
    }

    public async Task<RolePermission> AddAsync(RolePermission rolePermission)
    {
        await _context.RolePermissions.AddAsync(rolePermission);
        await _context.SaveChangesAsync();
        return rolePermission;
    }

    public async Task UpdateAsync(RolePermission rolePermission)
    {
        _context.RolePermissions.Update(rolePermission);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var rolePermission = await _context.RolePermissions.FindAsync(id);
        if (rolePermission != null)
        {
            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByRoleAndPermissionAsync(UserRole role, Guid permissionId)
    {
        var rolePermission = await _context.RolePermissions
            .FirstOrDefaultAsync(rp => rp.Role == role && rp.PermissionId == permissionId);

        if (rolePermission != null)
        {
            _context.RolePermissions.Remove(rolePermission);
            await _context.SaveChangesAsync();
        }
    }
}
