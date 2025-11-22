using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class ReportService : IReportService
{
    private readonly IEvaluationRepository _evaluationRepository;
    private readonly IAssignmentRepository _assignmentRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ApplicationDbContext _context;

    public ReportService(
        IEvaluationRepository evaluationRepository,
        IAssignmentRepository assignmentRepository,
        IProjectRepository projectRepository,
        ApplicationDbContext context)
    {
        _evaluationRepository = evaluationRepository;
        _assignmentRepository = assignmentRepository;
        _projectRepository = projectRepository;
        _context = context;
    }

    public async Task<EvaluationReportDto?> GetEvaluationReportAsync(Guid evaluationId)
    {
        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Branch)
            .Include(e => e.Evaluator)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
                    .ThenInclude(q => q.Section)
            .FirstOrDefaultAsync(e => e.Id == evaluationId);

        if (evaluation == null)
            return null;

        var sectionScores = evaluation.Answers
            .GroupBy(a => a.Question.Section)
            .Select(g => new SectionScoreDto
            {
                SectionName = g.Key.Name,
                Score = (int)g.Sum(a => a.EarnedPoints ?? 0),
                MaxScore = g.Sum(a => a.Question.Points),
                Percentage = g.Sum(a => a.Question.Points) > 0
                    ? (double)g.Sum(a => a.EarnedPoints ?? 0) / g.Sum(a => a.Question.Points) * 100
                    : 0
            })
            .ToList();

        return new EvaluationReportDto
        {
            EvaluationId = evaluation.Id,
            AssignmentId = evaluation.AssignmentId,
            BranchName = evaluation.Assignment.Branch?.Name ?? "N/A",
            BranchCity = evaluation.Assignment.Branch?.City ?? "N/A",
            EvaluatorName = evaluation.Evaluator != null
                ? $"{evaluation.Evaluator.FirstName} {evaluation.Evaluator.LastName}"
                : "External",
            CompletedAt = evaluation.CompletedAt ?? DateTime.UtcNow,
            TotalScore = (int)(evaluation.TotalScore ?? 0),
            MaxScore = (int)(evaluation.MaxScore ?? 0),
            PercentageScore = (double)(evaluation.ScorePercentage ?? 0),
            SectionScores = sectionScores
        };
    }

    public async Task<BranchPerformanceReportDto?> GetBranchPerformanceReportAsync(
        Guid branchId,
        ReportFilterDto? filter = null)
    {
        var branch = await _context.Branches
            .FirstOrDefaultAsync(b => b.Id == branchId);

        if (branch == null)
            return null;

        var query = _context.Evaluations
            .Include(e => e.Assignment)
            .Include(e => e.Evaluator)
            .Where(e => e.Assignment.BranchId == branchId && e.CompletedAt != null);

        if (filter != null)
        {
            if (filter.StartDate.HasValue)
                query = query.Where(e => e.CompletedAt >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                query = query.Where(e => e.CompletedAt <= filter.EndDate.Value);

            if (filter.ProjectId.HasValue)
                query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);
        }

        var evaluations = await query.ToListAsync();

        var recentEvaluations = evaluations
            .OrderByDescending(e => e.CompletedAt)
            .Take(10)
            .Select(e => new EvaluationSummaryDto
            {
                EvaluationId = e.Id,
                CompletedAt = e.CompletedAt ?? DateTime.UtcNow,
                Score = (int)(e.TotalScore ?? 0),
                MaxScore = (int)(e.MaxScore ?? 0),
                EvaluatorName = e.Evaluator != null
                    ? $"{e.Evaluator.FirstName} {e.Evaluator.LastName}"
                    : "External"
            })
            .ToList();

        return new BranchPerformanceReportDto
        {
            BranchId = branch.Id,
            BranchName = branch.Name,
            City = branch.City ?? "N/A",
            Region = branch.Region ?? "N/A",
            TotalEvaluations = evaluations.Count,
            AverageScore = evaluations.Any() ? evaluations.Average(e => (double)(e.ScorePercentage ?? 0)) : 0,
            MinScore = evaluations.Any() ? evaluations.Min(e => (double)(e.ScorePercentage ?? 0)) : 0,
            MaxScore = evaluations.Any() ? evaluations.Max(e => (double)(e.ScorePercentage ?? 0)) : 0,
            RecentEvaluations = recentEvaluations
        };
    }

    public async Task<ProjectReportDto?> GetProjectReportAsync(Guid projectId)
    {
        var project = await _context.Projects
            .Include(p => p.Assignments)
                .ThenInclude(a => a.Evaluations)
            .Include(p => p.Assignments)
                .ThenInclude(a => a.Branch)
            .FirstOrDefaultAsync(p => p.Id == projectId);

        if (project == null)
            return null;

        var completedAssignments = project.Assignments.Count(a => a.IsCompleted);
        var pendingAssignments = project.Assignments.Count(a => !a.IsCompleted);

        var branchScores = project.Assignments
            .Where(a => a.Evaluations.Any() && a.Branch != null)
            .GroupBy(a => a.Branch)
            .Select(g => new BranchScoreSummaryDto
            {
                BranchId = g.Key!.Id,
                BranchName = g.Key.Name,
                EvaluationCount = g.Count(),
                AverageScore = g.Average(a => (double)(a.Evaluations.First().ScorePercentage ?? 0))
            })
            .ToList();

        var evaluations = project.Assignments
            .Where(a => a.Evaluations.Any())
            .SelectMany(a => a.Evaluations)
            .ToList();

        return new ProjectReportDto
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            StartDate = project.StartDate,
            EndDate = project.EndDate,
            TotalAssignments = project.Assignments.Count,
            CompletedAssignments = completedAssignments,
            PendingAssignments = pendingAssignments,
            CompletionRate = project.Assignments.Count > 0
                ? (double)completedAssignments / project.Assignments.Count * 100
                : 0,
            AverageScore = evaluations.Any() ? evaluations.Average(e => (double)(e.ScorePercentage ?? 0)) : 0,
            BranchScores = branchScores
        };
    }

    public async Task<IEnumerable<EvaluationReportDto>> GetEvaluationReportsAsync(ReportFilterDto filter)
    {
        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Branch)
            .Include(e => e.Evaluator)
            .Include(e => e.Answers)
                .ThenInclude(a => a.Question)
                    .ThenInclude(q => q.Section)
            .Where(e => e.CompletedAt != null);

        if (filter.StartDate.HasValue)
            query = query.Where(e => e.CompletedAt >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(e => e.CompletedAt <= filter.EndDate.Value);

        if (filter.BranchId.HasValue)
            query = query.Where(e => e.Assignment.BranchId == filter.BranchId.Value);

        if (filter.ProjectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == filter.ProjectId.Value);

        if (filter.EvaluatorId.HasValue)
            query = query.Where(e => e.EvaluatorId == filter.EvaluatorId.Value);

        var evaluations = await query.ToListAsync();

        return evaluations.Select(evaluation =>
        {
            var sectionScores = evaluation.Answers
                .GroupBy(a => a.Question.Section)
                .Select(g => new SectionScoreDto
                {
                    SectionName = g.Key.Name,
                    Score = (int)g.Sum(a => a.EarnedPoints ?? 0),
                    MaxScore = g.Sum(a => a.Question.Points),
                    Percentage = g.Sum(a => a.Question.Points) > 0
                        ? (double)g.Sum(a => a.EarnedPoints ?? 0) / g.Sum(a => a.Question.Points) * 100
                        : 0
                })
                .ToList();

            return new EvaluationReportDto
            {
                EvaluationId = evaluation.Id,
                AssignmentId = evaluation.AssignmentId,
                BranchName = evaluation.Assignment.Branch?.Name ?? "N/A",
                BranchCity = evaluation.Assignment.Branch?.City ?? "N/A",
                EvaluatorName = evaluation.Evaluator != null
                    ? $"{evaluation.Evaluator.FirstName} {evaluation.Evaluator.LastName}"
                    : "External",
                CompletedAt = evaluation.CompletedAt ?? DateTime.UtcNow,
                TotalScore = (int)(evaluation.TotalScore ?? 0),
                MaxScore = (int)(evaluation.MaxScore ?? 0),
                PercentageScore = (double)(evaluation.ScorePercentage ?? 0),
                SectionScores = sectionScores
            };
        });
    }
}
