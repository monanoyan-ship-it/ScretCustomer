using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class ProjectFileDataService : IProjectFileDataService
{
    private readonly ApplicationDbContext _context;

    public ProjectFileDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectFile?> GetByIdAsync(int id)
        => await _context.ProjectFiles.FindAsync(id);

    public async Task<List<ProjectFile>> GetAllAsync()
        => await _context.ProjectFiles.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(ProjectFile entity)
    {
        _context.ProjectFiles.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProjectFile entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.ProjectFiles.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.ProjectFiles.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
