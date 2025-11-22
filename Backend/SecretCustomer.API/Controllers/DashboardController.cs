using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Dashboard;
using SecretCustomer.Core.Interfaces.Services;
using static SecretCustomer.Core.Interfaces.Services.IDashboardService;

namespace SecretCustomer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(
        IDashboardService dashboardService,
        ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    /// <summary>
    /// Admin Dashboard - Tüm veriler
    /// </summary>
    [HttpGet("admin")]
    public async Task<ActionResult<DashboardStatsDto>> GetAdminDashboard(
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var stats = await _dashboardService.GetAdminDashboardAsync(startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting admin dashboard");
            return StatusCode(500, "An error occurred while retrieving dashboard data");
        }
    }

    /// <summary>
    /// Team Leader Dashboard - Şube bazlı veriler
    /// </summary>
    [HttpGet("teamleader/{branchId}")]
    public async Task<ActionResult<DashboardStatsDto>> GetTeamLeaderDashboard(
        Guid branchId,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var stats = await _dashboardService.GetTeamLeaderDashboardAsync(branchId, startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting team leader dashboard for branch {BranchId}", branchId);
            return StatusCode(500, "An error occurred while retrieving dashboard data");
        }
    }

    /// <summary>
    /// Representative Dashboard - Kullanıcı bazlı değerlendirmeler
    /// </summary>
    [HttpGet("representative/{userId}")]
    public async Task<ActionResult<List<RepresentativeEvaluationDto>>> GetRepresentativeDashboard(Guid userId)
    {
        try
        {
            var evaluations = await _dashboardService.GetRepresentativeDashboardAsync(userId);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting representative dashboard for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving dashboard data");
        }
    }
}
