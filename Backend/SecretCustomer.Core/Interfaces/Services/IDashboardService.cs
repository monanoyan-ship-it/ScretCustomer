using SecretCustomer.Core.DTOs.Dashboard;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetAdminDashboardAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<DashboardStatsDto> GetTeamLeaderDashboardAsync(int branchId, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<RepresentativeEvaluationDto>> GetRepresentativeDashboardAsync(int userId);
    Task<ScorecardDto> GetScorecardAsync(int userId);

    // Yeni metrikler
    Task<DailyMetricsDto> GetDailyMetricsAsync(int? userId = null);
    Task<UserPerformanceDto> GetUserPerformanceAsync(int? currentUserId = null);
    Task<TargetProgressDto> GetTargetProgressAsync(int? userId = null);

    // Kullanıcı proje kırılımı (Bu ay en çok dinleyenler detayı)
    Task<UserProjectBreakdownDto> GetUserProjectBreakdownAsync(int userId);

    // Firma bazlı aylık trend
    Task<List<CustomerMonthlyTrendDto>> GetCustomerMonthlyTrendAsync();
}

public class RepresentativeEvaluationDto
{
    public int Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ChecklistName { get; set; } = string.Empty;
    public decimal? ScorePercentage { get; set; }
    public DateTime? CompletedAt { get; set; }
}
