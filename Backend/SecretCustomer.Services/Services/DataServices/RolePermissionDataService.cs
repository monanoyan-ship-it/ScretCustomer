using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class RolePermissionDataService : IRolePermissionDataService
{
    private readonly ApplicationDbContext _context;

    public RolePermissionDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RolePermission?> GetByIdAsync(int id)
        => await _context.RolePermissions.FindAsync(id);

    public async Task<List<RolePermission>> GetAllAsync()
        => await _context.RolePermissions.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(RolePermission entity)
    {
        _context.RolePermissions.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(RolePermission entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.RolePermissions.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.RolePermissions.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
