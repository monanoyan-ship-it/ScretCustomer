using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public class DashboardApiController : BaseApiController
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public DashboardApiController(
        IDashboardService dashboardService,
        ILogger<DashboardApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Dashboard.LoadError"), ex));
        }
    }

    [HttpGet("team-leader/{branchId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetTeamLeaderDashboard(
        int branchId,
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Dashboard.LoadError"), ex));
        }
    }

    [HttpGet("representative")]
    [Authorize(Roles = "CustomerRepresentative,Evaluator")]
    public async Task<IActionResult> GetRepresentativeDashboard()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.UserNotFound")));
            }

            var evaluations = await _dashboardService.GetRepresentativeDashboardAsync(userId);
            return Ok(evaluations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading representative dashboard");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Dashboard.LoadError"), ex));
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
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Common.UserNotFound")));
            }

            var scorecard = await _dashboardService.GetScorecardAsync(userId);
            return Ok(scorecard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading scorecard");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Dashboard.ScorecardLoadError"), ex));
        }
    }

    /// <summary>
    /// Günlük dinleme metrikleri
    /// </summary>
    [HttpGet("daily-metrics")]
    public async Task<IActionResult> GetDailyMetrics()
    {
        try
        {
            var metrics = await _dashboardService.GetDailyMetricsAsync();
            return Ok(metrics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading daily metrics");
            return StatusCode(500, CreateErrorResponse("Günlük metrikler yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Kullanıcı performans metrikleri
    /// </summary>
    [HttpGet("user-performance")]
    public async Task<IActionResult> GetUserPerformance()
    {
        try
        {
            int? currentUserId = null;
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var userId))
            {
                currentUserId = userId;
            }

            var performance = await _dashboardService.GetUserPerformanceAsync(currentUserId);
            return Ok(performance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user performance");
            return StatusCode(500, CreateErrorResponse("Kullanıcı performansı yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Hedef takip metrikleri
    /// </summary>
    [HttpGet("target-progress")]
    public async Task<IActionResult> GetTargetProgress()
    {
        try
        {
            var progress = await _dashboardService.GetTargetProgressAsync();
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading target progress");
            return StatusCode(500, CreateErrorResponse("Hedef bilgileri yüklenirken hata oluştu", ex));
        }
    }
}
