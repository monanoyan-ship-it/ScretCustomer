using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services.DataServices;

public class ProjectDataService : IProjectDataService
{
    private readonly ApplicationDbContext _context;

    public ProjectDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Project?> GetByIdAsync(int id)
        => await _context.Projects.FindAsync(id);

    public async Task<List<Project>> GetAllAsync()
        => await _context.Projects.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(Project entity)
    {
        _context.Projects.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(Project entity)
    {
        entity.UpdatedAt = DateTime.UtcNow;
        _context.Projects.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.Projects.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }
}
