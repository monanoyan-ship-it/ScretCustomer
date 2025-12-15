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
    private readonly ILocalizationService _localizationService;

    public DashboardApiController(
        IDashboardService dashboardService,
        ILogger<DashboardApiController> logger,
        ILocalizationService localizationService)
    {
        _dashboardService = dashboardService;
        _logger = logger;
        _localizationService = localizationService;
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
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Dashboard.LoadError") });
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
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Dashboard.LoadError") });
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
                return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.Common.UserNotFound") });
            }

            var evaluations = await _dashboardService.GetRepresentativeDashboardAsync(userId);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading representative dashboard");
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Dashboard.LoadError") });
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
                return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.Common.UserNotFound") });
            }

            var scorecard = await _dashboardService.GetScorecardAsync(userId);
            return Ok(scorecard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading scorecard");
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Dashboard.ScorecardLoadError") });
        }
    }
}
