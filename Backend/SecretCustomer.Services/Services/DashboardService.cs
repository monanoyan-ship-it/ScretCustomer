using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Dashboard;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using System.Globalization;
using static SecretCustomer.Core.Interfaces.Services.IDashboardService;

namespace SecretCustomer.Services.Services;

public class DashboardService : IDashboardService
{
    private readonly IEvaluationRepository _evaluationRepository;
    private readonly IUserRepository _userRepository;

    public DashboardService(IEvaluationRepository evaluationRepository, IUserRepository userRepository)
    {
        _evaluationRepository = evaluationRepository;
        _userRepository = userRepository;
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

        // Branch system removed - return empty lists
        var topBranches = new List<TopBranchDto>();
        var bottomBranches = new List<TopBranchDto>();

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

        // Branch system removed - return empty list
        var branchComparisons = new List<BranchComparisonDto>();

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

    public async Task<DashboardStatsDto> GetTeamLeaderDashboardAsync(int branchId, DateTime? startDate = null, DateTime? endDate = null)
    {
        // Branch system removed - return admin dashboard instead
        return await GetAdminDashboardAsync(startDate, endDate);
    }

    public async Task<List<RepresentativeEvaluationDto>> GetRepresentativeDashboardAsync(int userId)
    {
        var evaluations = await _evaluationRepository.GetByEvaluatorIdAsync(userId);

        return evaluations
            .Where(e => e.ScorePercentage.HasValue)
            .Select(e => new RepresentativeEvaluationDto
            {
                Id = e.Id,
                ProjectName = e.Assignment?.Project?.Name ?? "",
                BranchName = "",
                ScorePercentage = e.ScorePercentage,
                CompletedAt = e.CompletedAt
            })
            .ToList();
    }

    public async Task<ScorecardDto> GetScorecardAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return new ScorecardDto();
        }

        var now = DateTime.UtcNow;
        var currentMonthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var lastMonthStart = currentMonthStart.AddMonths(-1);
        var lastMonthEnd = currentMonthStart.AddDays(-1);

        // Kullanıcının değerlendirmeleri
        var userEvaluations = await _evaluationRepository.GetByEvaluatorIdAsync(userId);
        var completedEvaluations = userEvaluations.Where(e => e.ScorePercentage.HasValue).ToList();

        // Bu ay
        var currentMonthEvals = completedEvaluations
            .Where(e => e.CompletedAt >= currentMonthStart)
            .ToList();
        var currentMonthCount = currentMonthEvals.Count;
        var currentMonthAverage = currentMonthEvals.Any()
            ? currentMonthEvals.Average(e => e.ScorePercentage ?? 0)
            : 0;

        // Geçen ay
        var lastMonthEvals = completedEvaluations
            .Where(e => e.CompletedAt >= lastMonthStart && e.CompletedAt < currentMonthStart)
            .ToList();
        var lastMonthCount = lastMonthEvals.Count;
        var lastMonthAverage = lastMonthEvals.Any()
            ? lastMonthEvals.Average(e => e.ScorePercentage ?? 0)
            : 0;

        // Toplam
        var totalCount = completedEvaluations.Count;
        var totalAverage = completedEvaluations.Any()
            ? completedEvaluations.Average(e => e.ScorePercentage ?? 0)
            : 0;

        // Değişim
        var monthlyChange = currentMonthAverage - lastMonthAverage;

        // Şirket ortalaması
        var allEvaluations = await _evaluationRepository.GetAllAsync(null, null);
        var allCompletedEvals = allEvaluations.Where(e => e.ScorePercentage.HasValue).ToList();
        var companyAverage = allCompletedEvals.Any()
            ? allCompletedEvals.Average(e => e.ScorePercentage ?? 0)
            : 0;

        // Takım ortalaması (aynı branch'teki kullanıcılar)
        var teamAverage = companyAverage; // Basitleştirilmiş - kullanıcının branch'ine göre düzenlenebilir

        // Kullanıcı sıralaması (toplam ortalamaya göre)
        var userAverages = allCompletedEvals
            .Where(e => e.EvaluatorId.HasValue)
            .GroupBy(e => e.EvaluatorId!.Value)
            .Select(g => new
            {
                UserId = g.Key,
                Average = g.Average(e => e.ScorePercentage ?? 0)
            })
            .OrderByDescending(u => u.Average)
            .ToList();

        var userRank = userAverages.FindIndex(u => u.UserId == userId) + 1;
        var totalUsers = userAverages.Count;

        // Son 5 değerlendirme
        var recentEvaluations = completedEvaluations
            .OrderByDescending(e => e.CompletedAt ?? e.CreatedAt)
            .Take(5)
            .Select(e => new RecentEvaluationDto
            {
                Id = e.Id,
                ProjectName = e.Assignment?.Project?.Name ?? "",
                BranchName = "",
                ScorePercentage = e.ScorePercentage,
                EvaluationDate = e.CompletedAt ?? e.CreatedAt,
                Status = e.Status.ToString()
            })
            .ToList();

        return new ScorecardDto
        {
            UserName = $"{user.FirstName} {user.LastName}",
            Role = user.Role.ToString(),
            CurrentMonthEvaluations = currentMonthCount,
            CurrentMonthAverage = Math.Round(currentMonthAverage, 2),
            LastMonthEvaluations = lastMonthCount,
            LastMonthAverage = Math.Round(lastMonthAverage, 2),
            TotalEvaluations = totalCount,
            TotalAverage = Math.Round(totalAverage, 2),
            MonthlyChange = Math.Round(monthlyChange, 2),
            TeamAverage = Math.Round(teamAverage, 2),
            CompanyAverage = Math.Round(companyAverage, 2),
            UserRank = userRank > 0 ? userRank : totalUsers,
            TotalUsers = totalUsers > 0 ? totalUsers : 1,
            RecentEvaluations = recentEvaluations
        };
    }
}
