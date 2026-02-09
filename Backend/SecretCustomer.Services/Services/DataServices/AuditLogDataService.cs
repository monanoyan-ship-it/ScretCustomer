using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class AuditLogDataService : IAuditLogDataService
{
    private readonly ApplicationDbContext _context;

    public AuditLogDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AuditLog?> GetByIdAsync(int id)
        => await _context.AuditLogs.FindAsync(id);

    public async Task<List<AuditLog>> GetAllAsync()
        => await _context.AuditLogs.ToListAsync();

    public async Task AddAsync(AuditLog entity)
    {
        _context.AuditLogs.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(AuditLog entity)
    {
        _context.AuditLogs.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.AuditLogs.FindAsync(id);
        if (entity != null)
        {
            _context.AuditLogs.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
