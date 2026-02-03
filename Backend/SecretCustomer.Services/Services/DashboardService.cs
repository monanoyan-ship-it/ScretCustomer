using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Dashboard;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Globalization;
using static SecretCustomer.Core.Interfaces.Services.IDashboardService;

namespace SecretCustomer.Services.Services;

public class DashboardService : IDashboardService
{
    private readonly IEvaluationRepository _evaluationRepository;
    private readonly IUserRepository _userRepository;
    private readonly ISystemSettingService _systemSettingService;
    private readonly ApplicationDbContext _context;

    public DashboardService(
        IEvaluationRepository evaluationRepository,
        IUserRepository userRepository,
        ISystemSettingService systemSettingService,
        ApplicationDbContext context)
    {
        _evaluationRepository = evaluationRepository;
        _userRepository = userRepository;
        _systemSettingService = systemSettingService;
        _context = context;
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
                ChecklistName = e.Assignment?.Checklist?.Name ?? "",
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
                ChecklistName = e.Assignment?.Checklist?.Name ?? "",
                ScorePercentage = e.ScorePercentage,
                EvaluationDate = e.CompletedAt ?? e.CreatedAt,
                Status = EvaluationStatuses.GetById(e.StatusId)?.SystemName ?? ""
            })
            .ToList();

        return new ScorecardDto
        {
            UserName = $"{user.FirstName} {user.LastName}",
            Role = UserRoles.GetById(user.RoleId)?.SystemName ?? "",
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

    /// <summary>
    /// Günlük dinleme metriklerini getirir
    /// </summary>
    public async Task<DailyMetricsDto> GetDailyMetricsAsync(int? userId = null)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var weekStart = today.AddDays(-(int)today.DayOfWeek + (int)DayOfWeek.Monday);
        if (today.DayOfWeek == DayOfWeek.Sunday) weekStart = weekStart.AddDays(-7);
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Günlük hedef
        var dailyTarget = await _systemSettingService.GetIntValueAsync(SystemSettingKeys.DailyEvaluationTarget, 55);

        // Base query
        var baseQuery = _context.Evaluations
            .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed);
        if (userId.HasValue)
            baseQuery = baseQuery.Where(e => e.EvaluatorId == userId.Value);

        // Bugün
        var todayEvaluations = await baseQuery
            .Where(e => e.CompletedAt.HasValue && e.CompletedAt.Value.Date == today)
            .ToListAsync();

        var todayCount = todayEvaluations.Count;
        var todayAverage = todayEvaluations.Any()
            ? todayEvaluations.Average(e => e.ScorePercentage ?? 0)
            : 0;

        // Bu hafta
        var weekEvaluations = await baseQuery
            .Where(e => e.CompletedAt.HasValue && e.CompletedAt.Value.Date >= weekStart && e.CompletedAt.Value.Date <= today)
            .ToListAsync();

        var weekCount = weekEvaluations.Count;
        var weekAverage = weekEvaluations.Any()
            ? weekEvaluations.Average(e => e.ScorePercentage ?? 0)
            : 0;

        // Bu ay
        var monthEvaluations = await baseQuery
            .Where(e => e.CompletedAt.HasValue && e.CompletedAt.Value >= monthStart)
            .CountAsync();

        // Günlük hedef yüzdesi
        var dailyPercentage = dailyTarget > 0 ? Math.Min(100, (decimal)todayCount / dailyTarget * 100) : 0;

        // Son 7 günün trend verileri
        var last7Days = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-6 + i))
            .ToList();

        var trendData = await baseQuery
            .Where(e => e.CompletedAt.HasValue && e.CompletedAt.Value.Date >= today.AddDays(-6))
            .GroupBy(e => e.CompletedAt!.Value.Date)
            .Select(g => new
            {
                Date = g.Key,
                Count = g.Count(),
                Average = g.Average(e => e.ScorePercentage ?? 0)
            })
            .ToListAsync();

        var dailyTrends = last7Days.Select(date =>
        {
            var data = trendData.FirstOrDefault(t => t.Date == date);
            return new DailyTrendDto
            {
                Date = date,
                DayName = CultureInfo.GetCultureInfo("tr-TR").DateTimeFormat.GetAbbreviatedDayName(date.DayOfWeek),
                EvaluationCount = data?.Count ?? 0,
                AverageScore = Math.Round(data?.Average ?? 0, 2)
            };
        }).ToList();

        return new DailyMetricsDto
        {
            TodayEvaluations = todayCount,
            ThisWeekEvaluations = weekCount,
            ThisMonthEvaluations = monthEvaluations,
            DailyTarget = dailyTarget,
            DailyTargetPercentage = Math.Round(dailyPercentage, 1),
            TodayAverageScore = Math.Round(todayAverage, 2),
            ThisWeekAverageScore = Math.Round(weekAverage, 2),
            DailyTrends = dailyTrends
        };
    }

    /// <summary>
    /// Kullanıcı performans metriklerini getirir
    /// </summary>
    public async Task<UserPerformanceDto> GetUserPerformanceAsync(int? currentUserId = null)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Bugün en çok değerlendirme yapanlar
        var topToday = await _context.Evaluations
            .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed && e.EvaluatorId.HasValue)
            .Where(e => e.CompletedAt.HasValue && e.CompletedAt.Value.Date == today)
            .GroupBy(e => e.EvaluatorId!.Value)
            .Select(g => new
            {
                UserId = g.Key,
                Count = g.Count(),
                Average = g.Average(e => e.ScorePercentage ?? 0)
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var topTodayUserIds = topToday.Select(t => t.UserId).ToList();
        var topTodayUsers = await _context.Users
            .Where(u => topTodayUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var topEvaluatorsToday = topToday.Select(t => new TopEvaluatorDto
        {
            UserId = t.UserId,
            UserName = topTodayUsers.GetValueOrDefault(t.UserId, "Bilinmeyen"),
            EvaluationCount = t.Count,
            AverageScore = Math.Round(t.Average, 2)
        }).ToList();

        // Bu ay en çok değerlendirme yapanlar
        var topMonth = await _context.Evaluations
            .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed && e.EvaluatorId.HasValue)
            .Where(e => e.CompletedAt.HasValue && e.CompletedAt.Value >= monthStart)
            .GroupBy(e => e.EvaluatorId!.Value)
            .Select(g => new
            {
                UserId = g.Key,
                Count = g.Count(),
                Average = g.Average(e => e.ScorePercentage ?? 0)
            })
            .OrderByDescending(x => x.Count)
            .Take(10)
            .ToListAsync();

        var topMonthUserIds = topMonth.Select(t => t.UserId).ToList();
        var topMonthUsers = await _context.Users
            .Where(u => topMonthUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var topEvaluatorsMonth = topMonth.Select(t => new TopEvaluatorDto
        {
            UserId = t.UserId,
            UserName = topMonthUsers.GetValueOrDefault(t.UserId, "Bilinmeyen"),
            EvaluationCount = t.Count,
            AverageScore = Math.Round(t.Average, 2)
        }).ToList();

        // Kullanıcı sıralaması (aylık)
        var rankData = await _context.Evaluations
            .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed && e.EvaluatorId.HasValue)
            .Where(e => e.CompletedAt.HasValue && e.CompletedAt.Value >= monthStart)
            .GroupBy(e => e.EvaluatorId!.Value)
            .Select(g => new
            {
                UserId = g.Key,
                Count = g.Count(),
                Average = g.Average(e => e.ScorePercentage ?? 0)
            })
            .OrderByDescending(x => x.Count)
            .ToListAsync();

        var allRankUserIds = rankData.Select(r => r.UserId).ToList();
        var allRankUsers = await _context.Users
            .Where(u => allRankUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => $"{u.FirstName} {u.LastName}");

        var userRankings = rankData.Select((r, index) => new UserRankingDto
        {
            Rank = index + 1,
            UserId = r.UserId,
            UserName = allRankUsers.GetValueOrDefault(r.UserId, "Bilinmeyen"),
            EvaluationCount = r.Count,
            AverageScore = Math.Round(r.Average, 2),
            IsCurrentUser = currentUserId.HasValue && r.UserId == currentUserId.Value
        }).ToList();

        return new UserPerformanceDto
        {
            TopEvaluatorsToday = topEvaluatorsToday,
            TopEvaluatorsMonth = topEvaluatorsMonth,
            UserRankings = userRankings
        };
    }

    /// <summary>
    /// Hedef takip metriklerini getirir
    /// </summary>
    public async Task<TargetProgressDto> GetTargetProgressAsync(int? userId = null)
    {
        var now = DateTime.UtcNow;
        var today = now.Date;

        // Günlük hedef
        var dailyTarget = await _systemSettingService.GetIntValueAsync(SystemSettingKeys.DailyEvaluationTarget, 55);

        // Base query
        var baseQuery = _context.Evaluations
            .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed);
        if (userId.HasValue)
            baseQuery = baseQuery.Where(e => e.EvaluatorId == userId.Value);

        // Bugün yapılanlar
        var todayCompleted = await baseQuery
            .Where(e => e.CompletedAt.HasValue && e.CompletedAt.Value.Date == today)
            .CountAsync();

        // Aktif dönem
        var activePeriod = await _context.AssignmentPeriods
            .Where(p => !p.IsDeleted && p.StatusId == PeriodStatuses.Ids.Open)
            .Where(p => p.StartDate <= now && p.EndDate >= now)
            .FirstOrDefaultAsync();

        string? periodName = null;
        DateTime? periodStart = null;
        DateTime? periodEnd = null;
        int periodTarget = 0;
        int periodCompleted = 0;
        decimal periodPercentage = 0;

        if (activePeriod != null)
        {
            periodName = activePeriod.Name;
            periodStart = activePeriod.StartDate;
            periodEnd = activePeriod.EndDate;
            periodTarget = activePeriod.TargetCount;

            // Dönemdeki tamamlanan değerlendirmeler
            var periodQuery = baseQuery
                .Where(e => e.CompletedAt.HasValue &&
                           e.CompletedAt.Value >= activePeriod.StartDate &&
                           e.CompletedAt.Value <= activePeriod.EndDate);
            periodCompleted = await periodQuery.CountAsync();

            periodPercentage = periodTarget > 0 ? Math.Min(100, (decimal)periodCompleted / periodTarget * 100) : 0;
        }

        // Proje bazlı hedefler (aktif projeler)
        var activeProjects = await _context.Projects
            .Where(p => !p.IsDeleted && p.IsActive)
            .Where(p => p.EndDate >= today)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.ChecklistId
            })
            .ToListAsync();

        var projectIds = activeProjects.Select(p => p.Id).ToList();

        // Her proje için tamamlanan değerlendirmeler
        var projectCompletedQuery = baseQuery
            .Where(e => e.Assignment != null)
            .Where(e => projectIds.Contains(e.Assignment!.ProjectId));
        var projectCompletedCounts = await projectCompletedQuery
            .GroupBy(e => e.Assignment!.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count);

        // Her proje için hedef (atama sayısı)
        var projectTargetCounts = await _context.Assignments
            .Where(a => !a.IsDeleted)
            .Where(a => projectIds.Contains(a.ProjectId))
            .GroupBy(a => a.ProjectId)
            .Select(g => new { ProjectId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ProjectId, x => x.Count);

        var projectTargets = activeProjects.Select(p =>
        {
            var completed = projectCompletedCounts.GetValueOrDefault(p.Id, 0);
            var target = projectTargetCounts.GetValueOrDefault(p.Id, 0);
            return new ProjectTargetDto
            {
                ProjectId = p.Id,
                ProjectName = p.Name,
                Target = target,
                Completed = completed,
                Percentage = target > 0 ? Math.Round((decimal)completed / target * 100, 1) : 0
            };
        }).ToList();

        return new TargetProgressDto
        {
            CurrentPeriodName = periodName,
            PeriodStartDate = periodStart,
            PeriodEndDate = periodEnd,
            PeriodTarget = periodTarget,
            PeriodCompleted = periodCompleted,
            PeriodPercentage = Math.Round(periodPercentage, 1),
            Remaining = Math.Max(0, periodTarget - periodCompleted),
            DailyTarget = dailyTarget,
            TodayCompleted = todayCompleted,
            ProjectTargets = projectTargets
        };
    }

    /// <summary>
    /// Kullanıcının bu ayki proje bazlı değerlendirme detayını getirir
    /// </summary>
    public async Task<UserProjectBreakdownDto> GetUserProjectBreakdownAsync(int userId)
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Kullanıcı bilgisi
        var user = await _context.Users.FindAsync(userId);
        var userName = user != null ? $"{user.FirstName} {user.LastName}" : "Bilinmeyen";

        // Bu ayki değerlendirmeler - proje bazlı gruplama
        var projectData = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a!.Project)
                    .ThenInclude(p => p!.Customer)
            .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed && e.EvaluatorId == userId)
            .Where(e => e.CompletedAt.HasValue && e.CompletedAt.Value >= monthStart)
            .GroupBy(e => new
            {
                ProjectId = e.Assignment!.ProjectId,
                ProjectName = e.Assignment.Project!.Name,
                ProjectCode = e.Assignment.Project.Code,
                CustomerName = e.Assignment.Project.Customer != null ? e.Assignment.Project.Customer.CompanyName : null
            })
            .Select(g => new UserProjectDetailDto
            {
                ProjectId = g.Key.ProjectId,
                ProjectName = g.Key.ProjectName,
                ProjectCode = g.Key.ProjectCode,
                CustomerName = g.Key.CustomerName,
                EvaluationCount = g.Count(),
                AverageScore = Math.Round(g.Average(e => e.ScorePercentage ?? 0), 2)
            })
            .OrderByDescending(p => p.EvaluationCount)
            .ToListAsync();

        var totalCount = projectData.Sum(p => p.EvaluationCount);

        return new UserProjectBreakdownDto
        {
            UserId = userId,
            UserName = userName,
            TotalEvaluations = totalCount,
            Projects = projectData
        };
    }

    /// <summary>
    /// Firma bazlı aylık trend verilerini getirir
    /// </summary>
    public async Task<List<CustomerMonthlyTrendDto>> GetCustomerMonthlyTrendAsync()
    {
        var now = DateTime.UtcNow;
        var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Firma bazlı gruplama
        var customerData = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a!.Project)
                    .ThenInclude(p => p!.Customer)
            .Where(e => !e.IsDeleted && e.StatusId == EvaluationStatuses.Ids.Completed)
            .Where(e => e.CompletedAt.HasValue && e.CompletedAt.Value >= monthStart)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId != null)
            .GroupBy(e => new
            {
                CustomerId = e.Assignment!.Project!.CustomerId!.Value,
                CustomerName = e.Assignment.Project.Customer!.CompanyName
            })
            .Select(g => new CustomerMonthlyTrendDto
            {
                CustomerId = g.Key.CustomerId,
                CustomerName = g.Key.CustomerName,
                EvaluationCount = g.Count(),
                AverageScore = Math.Round(g.Average(e => e.ScorePercentage ?? 0), 2)
            })
            .OrderByDescending(c => c.EvaluationCount)
            .ToListAsync();

        return customerData;
    }
}
