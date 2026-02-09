using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class LanguageDataService : ILanguageDataService
{
    private readonly ApplicationDbContext _context;

    public LanguageDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Language?> GetByIdAsync(int id)
        => await _context.Languages.FindAsync(id);

    public async Task<List<Language>> GetAllAsync()
        => await _context.Languages.ToListAsync();

    public async Task AddAsync(Language entity)
    {
        _context.Languages.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Language entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Languages.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Languages.FindAsync(id);
        if (entity != null)
        {
            _context.Languages.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
}
