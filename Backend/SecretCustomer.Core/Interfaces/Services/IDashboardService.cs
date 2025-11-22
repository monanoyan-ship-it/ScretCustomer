using SecretCustomer.Core.DTOs.Dashboard;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetAdminDashboardAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<DashboardStatsDto> GetTeamLeaderDashboardAsync(Guid branchId, DateTime? startDate = null, DateTime? endDate = null);
    Task<List<RepresentativeEvaluationDto>> GetRepresentativeDashboardAsync(Guid userId);
}

public class RepresentativeEvaluationDto
{
    public Guid Id { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string BranchName { get; set; } = string.Empty;
    public decimal? ScorePercentage { get; set; }
    public DateTime? CompletedAt { get; set; }
}
