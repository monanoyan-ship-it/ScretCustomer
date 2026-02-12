using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services.DataServices;
using SecretCustomer.Data;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services.DataServices;

public class ProjectTeamMemberDataService : IProjectTeamMemberDataService
{
    private readonly ApplicationDbContext _context;

    public ProjectTeamMemberDataService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProjectTeamMember?> GetByIdAsync(int id)
        => await _context.ProjectTeamMembers.FindAsync(id);

    public async Task<List<ProjectTeamMember>> GetAllAsync()
        => await _context.ProjectTeamMembers.Where(x => !x.IsDeleted).ToListAsync();

    public async Task AddAsync(ProjectTeamMember entity)
    {
        _context.ProjectTeamMembers.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateAsync(ProjectTeamMember entity)
    {
        entity.UpdatedAt = TurkeyTime.Now;
        _context.ProjectTeamMembers.Update(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var entity = await _context.ProjectTeamMembers.FindAsync(id);
        if (entity != null)
        {
            entity.IsDeleted = true;
            entity.UpdatedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }
    }
}
