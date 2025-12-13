using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardApiController : ControllerBase
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardApiController> _logger;

    public DashboardApiController(IDashboardService dashboardService, ILogger<DashboardApiController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAdminDashboard([FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var stats = await _dashboardService.GetAdminDashboardAsync(startDate, endDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading admin dashboard");
            return StatusCode(500, new { message = "Dashboard verileri yüklenirken bir hata oluştu." });
        }
    }

    [HttpGet("team-leader/{branchId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetTeamLeaderDashboard(
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
            _logger.LogError(ex, "Error loading team leader dashboard for branch {BranchId}", branchId);
            return StatusCode(500, new { message = "Dashboard verileri yüklenirken bir hata oluştu." });
        }
    }

    [HttpGet("representative")]
    [Authorize(Roles = "CustomerRepresentative,Evaluator")]
    public async Task<IActionResult> GetRepresentativeDashboard()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Kullanıcı bilgisi bulunamadı." });
            }

            var evaluations = await _dashboardService.GetRepresentativeDashboardAsync(userId);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading representative dashboard");
            return StatusCode(500, new { message = "Dashboard verileri yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Kişisel performans kartı (Scorecard)
    /// </summary>
    [HttpGet("scorecard")]
    public async Task<IActionResult> GetScorecard()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Kullanıcı bilgisi bulunamadı." });
            }

            var scorecard = await _dashboardService.GetScorecardAsync(userId);
            return Ok(scorecard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading scorecard");
            return StatusCode(500, new { message = "Scorecard verileri yüklenirken bir hata oluştu." });
        }
    }
}
