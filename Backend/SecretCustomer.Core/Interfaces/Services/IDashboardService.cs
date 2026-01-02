using SecretCustomer.Core.DTOs.Dashboard;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetAdminDashboardAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<DashboardStatsDto> GetTeamLeaderDashboardAsync(int branchId, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<RepresentativeEvaluationDto>> GetRepresentativeDashboardAsync(int userId);
    Task<ScorecardDto> GetScorecardAsync(int userId);

    // Yeni metrikler
    Task<DailyMetricsDto> GetDailyMetricsAsync();
    Task<UserPerformanceDto> GetUserPerformanceAsync(int? currentUserId = null);
    Task<TargetProgressDto> GetTargetProgressAsync();
}

public class RepresentativeEvaluationDto
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ChecklistName { get; set; } = string.Empty;
    public decimal? ScorePercentage { get; set; }
    public DateTime? CompletedAt { get; set; }
}
