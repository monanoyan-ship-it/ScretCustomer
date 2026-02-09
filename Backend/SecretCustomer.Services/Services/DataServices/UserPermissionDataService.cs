using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class UserPermissionDataService : IUserPermissionDataService
{
    private readonly ApplicationDbContext _context;

    public UserPermissionDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserPermission?> GetByIdAsync(int id)
        => await _context.UserPermissions.FindAsync(id);

    public async Task<List<UserPermission>> GetAllAsync()
        => await _context.UserPermissions.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(UserPermission entity)
    {
        _context.UserPermissions.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(UserPermission entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.UserPermissions.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.UserPermissions.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
