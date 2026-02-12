using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class ExcelColumnDataService : IExcelColumnDataService
{
    private readonly ApplicationDbContext _context;

    public ExcelColumnDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ExcelColumn?> GetByIdAsync(int id)
        => await _context.ExcelColumns.FindAsync(id);

    public async Task<List<ExcelColumn>> GetAllAsync()
        => await _context.ExcelColumns.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(ExcelColumn entity)
    {
        _context.ExcelColumns.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ExcelColumn entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.ExcelColumns.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.ExcelColumns.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
