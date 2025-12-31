using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class AssignmentRepository : IAssignmentRepository
{
    private readonly ApplicationDbContext _context;

    public AssignmentRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Assignment?> GetByIdAsync(int id, bool includeDetails = false)
    {
        var query = _context.Assignments.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(a => a.Project)
                .Include(a => a.Checklist)
                .Include(a => a.AssignedUser);
        }

        return await query.FirstOrDefaultAsync(a => a.Id == id);
    }

    public async Task<IEnumerable<Assignment>> GetAllAsync()
    {
        return await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Include(a => a.AssignedUser)
            .Where(a => !a.IsDeleted)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();
    }

    public async Task<Assignment?> GetByUniqueLinkAsync(string uniqueLink, bool includeDetails = false)
    {
        var query = _context.Assignments.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(a => a.Project)
                .Include(a => a.Checklist)
                    .ThenInclude(c => c.Sections.OrderBy(s => s.Order))
                        .ThenInclude(s => s.Questions.OrderBy(q => q.Order));
        }

        return await query.FirstOrDefaultAsync(a => a.UniqueLink == uniqueLink);
    }

    public async Task<IEnumerable<Assignment>> GetByProjectIdAsync(int projectId)
    {
        return await _context.Assignments
            .Include(a => a.AssignedUser)
            .Where(a => a.ProjectId == projectId)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }

    public async Task<IEnumerable<Assignment>> GetByUserIdAsync(int userId)
    {
        return await _context.Assignments
            .Include(a => a.Project)
            .Include(a => a.Checklist)
            .Where(a => a.AssignedUserId == userId)
            .OrderBy(a => a.DueDate)
            .ToListAsync();
    }


    public async Task<Assignment> CreateAsync(Assignment assignment)
    {
        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<IEnumerable<Assignment>> CreateBulkAsync(IEnumerable<Assignment> assignments)
    {
        _context.Assignments.AddRange(assignments);
        await _context.SaveChangesAsync();
        return assignments;
    }

    public async Task<Assignment> UpdateAsync(Assignment assignment)
    {
        _context.Assignments.Update(assignment);
        await _context.SaveChangesAsync();
        return assignment;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var assignment = await _context.Assignments.FindAsync(id);
        if (assignment == null) return false;

        assignment.IsDeleted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByEmailAsync(int projectId, string email)
    {
        return await _context.Assignments
            .AnyAsync(a => a.ProjectId == projectId && a.ExternalEmail == email && !a.IsCompleted);
    }
}
