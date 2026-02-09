using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class ExcelTemplateDataService : IExcelTemplateDataService
{
    private readonly ApplicationDbContext _context;

    public ExcelTemplateDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExcelTemplate?> GetByIdAsync(int id)
        => await _context.ExcelTemplates.FindAsync(id);

    public async Task<List<ExcelTemplate>> GetAllAsync()
        => await _context.ExcelTemplates.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(ExcelTemplate entity)
    {
        _context.ExcelTemplates.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ExcelTemplate entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.ExcelTemplates.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.ExcelTemplates.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
