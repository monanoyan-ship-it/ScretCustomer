using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class PermissionDataService : IPermissionDataService
{
    private readonly ApplicationDbContext _context;

    public PermissionDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Permission?> GetByIdAsync(int id)
        => await _context.Permissions.FindAsync(id);

    public async Task<List<Permission>> GetAllAsync()
        => await _context.Permissions.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(Permission entity)
    {
        _context.Permissions.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Permission entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.Permissions.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Permissions.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
