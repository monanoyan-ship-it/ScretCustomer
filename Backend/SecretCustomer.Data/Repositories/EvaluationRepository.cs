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

    public async Task<Evaluation?> GetByIdAsync(int id, bool includeDetails = false)
    {
        var query = _context.Evaluations.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(e => e.Project)
                .Include(e => e.Evaluator)
                .Include(e => e.EvaluatorCustomerPersonnel)
                .Include(e => e.Answers)
                    .ThenInclude(a => a.Question)
                .Include(e => e.Answers)
                    .ThenInclude(a => a.SubCriteriaSelections)
                        .ThenInclude(s => s.SubCriteria);
        }

        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<Evaluation?> GetByProjectIdAsync(int projectId, bool includeDetails = false)
    {
        var query = _context.Evaluations.AsQueryable();

        if (includeDetails)
        {
            query = query
                .Include(e => e.Answers)
                    .ThenInclude(a => a.Question);
        }

        return await query.FirstOrDefaultAsync(e => e.ProjectId == projectId);
    }

    public async Task<IEnumerable<Evaluation>> GetByEvaluatorIdAsync(int evaluatorId)
    {
        return await _context.Evaluations
            .Include(e => e.Project)
                .ThenInclude(p => p.Checklist)
            .Include(e => e.EvaluatedPersonnel)
            .Where(e => e.EvaluatorId == evaluatorId)
            .OrderByDescending(e => e.CompletedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Evaluation>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.Evaluations
            .Include(e => e.Project)
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
