using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Dashboard;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using System.Globalization;
using static SecretCustomer.Core.Interfaces.Services.IDashboardService;

namespace SecretCustomer.Services.Services;

public class DashboardService : IDashboardService
{
    private readonly IEvaluationRepository _evaluationRepository;

    public DashboardService(IEvaluationRepository evaluationRepository)
    {
        _evaluationRepository = evaluationRepository;
    }

    public async Task<DashboardStatsDto> GetAdminDashboardAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var evaluations = await _evaluationRepository.GetAllAsync(startDate, endDate);
        var completedEvaluations = evaluations.Where(e => e.ScorePercentage.HasValue).ToList();

        if (!completedEvaluations.Any())
            return new DashboardStatsDto();

        var totalEvaluations = completedEvaluations.Count;
        var averageScore = completedEvaluations.Average(e => e.ScorePercentage ?? 0);

        // Önceki ay karşılaştırması
        var previousMonth = DateTime.UtcNow.AddMonths(-1);
        var previousMonthEvals = await _evaluationRepository.GetAllAsync(
            previousMonth.AddMonths(-1), previousMonth);
        var previousMonthCompleted = previousMonthEvals.Where(e => e.ScorePercentage.HasValue).ToList();
        var previousAverage = previousMonthCompleted.Any()
            ? previousMonthCompleted.Average(e => e.ScorePercentage ?? 0)
            : 0;
        var percentageChange = previousAverage > 0
            ? ((averageScore - previousAverage) / previousAverage) * 100
            : 0;

        // Şube gruplandırması
        var branchGroups = completedEvaluations
            .Where(e => e.Assignment?.Branch != null)
            .GroupBy(e => e.Assignment.Branch)
            .Select(g => new
            {
                Branch = g.Key,
                AverageScore = g.Average(e => e.ScorePercentage ?? 0),
                Count = g.Count()
            })
            .ToList();

        var topBranches = branchGroups
            .OrderByDescending(b => b.AverageScore)
            .Take(5)
            .Select(b => new TopBranchDto
            {
                BranchId = b.Branch.Id,
                BranchName = b.Branch.Name,
                AverageScore = Math.Round(b.AverageScore, 2),
                EvaluationCount = b.Count
            })
            .ToList();

        var bottomBranches = branchGroups
            .OrderBy(b => b.AverageScore)
            .Take(5)
            .Select(b => new TopBranchDto
            {
                BranchId = b.Branch.Id,
                BranchName = b.Branch.Name,
                AverageScore = Math.Round(b.AverageScore, 2),
                EvaluationCount = b.Count
            })
            .ToList();

        // Son 12 ay trend
        var last12Months = completedEvaluations
            .Where(e => e.CompletedAt.HasValue)
            .GroupBy(e => new { e.CompletedAt!.Value.Year, e.CompletedAt!.Value.Month })
            .Select(g => new MonthlyTrendDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetMonthName(g.Key.Month),
                AverageScore = Math.Round(g.Average(e => e.ScorePercentage ?? 0), 2),
                EvaluationCount = g.Count()
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .TakeLast(12)
            .ToList();

        // Şube karşılaştırması
        var branchComparisons = branchGroups
            .Select(b => new BranchComparisonDto
            {
                BranchId = b.Branch.Id,
                BranchName = b.Branch.Name,
                Region = b.Branch.Region ?? "Belirtilmemiş",
                AverageScore = Math.Round(b.AverageScore, 2),
                EvaluationCount = b.Count
            })
            .OrderByDescending(b => b.AverageScore)
            .ToList();

        return new DashboardStatsDto
        {
            TotalEvaluations = totalEvaluations,
            AverageScore = Math.Round(averageScore, 2),
            PercentageChange = Math.Round(percentageChange, 2),
            TopBranches = topBranches,
            BottomBranches = bottomBranches,
            MonthlyTrends = last12Months,
            BranchComparisons = branchComparisons
        };
    }

    public async Task<DashboardStatsDto> GetTeamLeaderDashboardAsync(Guid branchId, DateTime? startDate = null, DateTime? endDate = null)
    {
        var evaluations = await _evaluationRepository.GetByBranchIdAsync(branchId, startDate, endDate);
        var completedEvaluations = evaluations.Where(e => e.ScorePercentage.HasValue).ToList();

        if (!completedEvaluations.Any())
            return new DashboardStatsDto();

        var totalEvaluations = completedEvaluations.Count;
        var averageScore = completedEvaluations.Average(e => e.ScorePercentage ?? 0);

        // Önceki ay karşılaştırması
        var previousMonth = DateTime.UtcNow.AddMonths(-1);
        var previousMonthEvals = await _evaluationRepository.GetByBranchIdAsync(
            branchId, previousMonth.AddMonths(-1), previousMonth);
        var previousMonthCompleted = previousMonthEvals.Where(e => e.ScorePercentage.HasValue).ToList();
        var previousAverage = previousMonthCompleted.Any()
            ? previousMonthCompleted.Average(e => e.ScorePercentage ?? 0)
            : 0;
        var percentageChange = previousAverage > 0
            ? ((averageScore - previousAverage) / previousAverage) * 100
            : 0;

        // Son 12 ay trend
        var last12Months = completedEvaluations
            .Where(e => e.CompletedAt.HasValue)
            .GroupBy(e => new { e.CompletedAt!.Value.Year, e.CompletedAt!.Value.Month })
            .Select(g => new MonthlyTrendDto
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                MonthName = CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetMonthName(g.Key.Month),
                AverageScore = Math.Round(g.Average(e => e.ScorePercentage ?? 0), 2),
                EvaluationCount = g.Count()
            })
            .OrderBy(m => m.Year).ThenBy(m => m.Month)
            .TakeLast(12)
            .ToList();

        return new DashboardStatsDto
        {
            TotalEvaluations = totalEvaluations,
            AverageScore = Math.Round(averageScore, 2),
            PercentageChange = Math.Round(percentageChange, 2),
            MonthlyTrends = last12Months
        };
    }

    public async Task<List<RepresentativeEvaluationDto>> GetRepresentativeDashboardAsync(Guid userId)
    {
        var evaluations = await _evaluationRepository.GetByEvaluatorIdAsync(userId);

        return evaluations
            .Where(e => e.ScorePercentage.HasValue)
            .Select(e => new RepresentativeEvaluationDto
            {
                Id = e.Id,
                ProjectName = e.Assignment?.Project?.Name ?? "",
                BranchName = e.Assignment?.Branch?.Name ?? "",
                ScorePercentage = e.ScorePercentage,
                CompletedAt = e.CompletedAt
            })
            .ToList();
    }
}
