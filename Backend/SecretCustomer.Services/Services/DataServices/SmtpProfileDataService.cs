using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class SmtpProfileDataService : ISmtpProfileDataService
{
    private readonly ApplicationDbContext _context;

    public SmtpProfileDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SmtpProfile?> GetByIdAsync(int id)
        => await _context.SmtpProfiles.FindAsync(id);

    public async Task<List<SmtpProfile>> GetAllAsync()
        => await _context.SmtpProfiles.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(SmtpProfile entity)
    {
        _context.SmtpProfiles.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(SmtpProfile entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.SmtpProfiles.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.SmtpProfiles.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
