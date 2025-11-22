using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;

namespace SecretCustomer.Data.Repositories;

public class EvaluationRepository : IEvaluationRepository
{
    private readonly ApplicationDbContext _context;

    public EvaluationRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Evaluation?> GetByIdAsync(Guid id, bool includeDetails = false)
    {
        var query = _context.Evaluations.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(e => e.Assignment)
                    .ThenInclude(a => a.Branch)
                .Include(e => e.Evaluator)
                .Include(e => e.Answers)
                    .ThenInclude(a => a.Question);
        }

        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Evaluation?> GetByAssignmentIdAsync(Guid assignmentId, bool includeDetails = false)
    {
        var query = _context.Evaluations.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(e => e.Answers)
                    .ThenInclude(a => a.Question);
        }

        return await query.FirstOrDefaultAsync(e => e.AssignmentId == assignmentId);
    }

    public async Task<IEnumerable<Evaluation>> GetByEvaluatorIdAsync(Guid evaluatorId)
    {
        return await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Branch)
            .Where(e => e.EvaluatorId == evaluatorId)
            .OrderByDescending(e => e.CompletedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Evaluation>> GetByBranchIdAsync(Guid branchId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Evaluations
            .Include(e => e.Assignment)
            .Where(e => e.Assignment.BranchId == branchId);

        if (startDate.HasValue)
            query = query.Where(e => e.CompletedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.CompletedAt <= endDate.Value);

        return await query.ToListAsync();
    }

    public async Task<IEnumerable<Evaluation>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Branch)
            .AsQueryable();

        if (startDate.HasValue)
            query = query.Where(e => e.CompletedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(e => e.CompletedAt <= endDate.Value);

        return await query.ToListAsync();
    }

    public async Task<Evaluation> CreateAsync(Evaluation evaluation)
    {
        _context.Evaluations.Add(evaluation);
        await _context.SaveChangesAsync();
        return evaluation;
    }

    public async Task<Evaluation> UpdateAsync(Evaluation evaluation)
    {
        _context.Evaluations.Update(evaluation);
        await _context.SaveChangesAsync();
        return evaluation;
    }
}
