using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class UserPermissionRepository : IUserPermissionRepository
{
    private readonly ApplicationDbContext _context;

    public UserPermissionRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserPermission?> GetByIdAsync(Guid id)
    {
        return await _context.UserPermissions
            .Include(up => up.Permission)
            .Include(up => up.User)
            .FirstOrDefaultAsync(up => up.Id == id);
    }

    public async Task<IEnumerable<UserPermission>> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserPermissions
            .Include(up => up.Permission)
            .Where(up => up.UserId == userId)
            .ToListAsync();
    }

    public async Task<UserPermission?> GetByUserAndPermissionAsync(Guid userId, Guid permissionId)
    {
        return await _context.UserPermissions
            .Include(up => up.Permission)
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);
    }

    public async Task<UserPermission> AddAsync(UserPermission userPermission)
    {
        await _context.UserPermissions.AddAsync(userPermission);
        await _context.SaveChangesAsync();
        return userPermission;
    }

    public async Task UpdateAsync(UserPermission userPermission)
    {
        _context.UserPermissions.Update(userPermission);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var userPermission = await _context.UserPermissions.FindAsync(id);
        if (userPermission != null)
        {
            _context.UserPermissions.Remove(userPermission);
            await _context.SaveChangesAsync();
        }
    }

    public async Task DeleteByUserAndPermissionAsync(Guid userId, Guid permissionId)
    {
        var userPermission = await _context.UserPermissions
            .FirstOrDefaultAsync(up => up.UserId == userId && up.PermissionId == permissionId);

        if (userPermission != null)
        {
            _context.UserPermissions.Remove(userPermission);
            await _context.SaveChangesAsync();
        }
    }
}
