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
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
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
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.NotFound")));
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
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.User.NotFound")));
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
    /// Admin: Tüm şirket verisi, Non-admin: Sadece kendi verileri
    /// </summary>
    [HttpGet("daily-metrics")]
    public async Task<IActionResult> GetDailyMetrics()
    {
        try
        {
            int? userId = null;
            if (!User.IsInRole("Admin"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedId))
                {
                    userId = parsedId;
                }
            }

            var metrics = await _dashboardService.GetDailyMetricsAsync(userId);
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
    /// Admin: Tüm şirket verisi, Non-admin: Sadece kendi verileri
    /// </summary>
    [HttpGet("target-progress")]
    public async Task<IActionResult> GetTargetProgress()
    {
        try
        {
            int? userId = null;
            if (!User.IsInRole("Admin"))
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var parsedId))
                {
                    userId = parsedId;
                }
            }

            var progress = await _dashboardService.GetTargetProgressAsync(userId);
            return Ok(progress);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading target progress");
            return StatusCode(500, CreateErrorResponse("Hedef bilgileri yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Müşteri personeli için değerlendirmeleri getirir (yıl ve ay filtreli)
    /// - CustomerOperator: Sadece kendi değerlendirmeleri
    /// - CustomerSupervisor: Takımındaki kişilerin değerlendirmeleri
    /// - CustomerManager: Tüm müşteri değerlendirmeleri
    /// </summary>
    [HttpGet("my-evaluations")]
    [Authorize(Roles = "CustomerManager,CustomerSupervisor,CustomerOperator")]
    public async Task<IActionResult> GetMyEvaluations([FromQuery] int? year = null, [FromQuery] int? month = null)
    {
        try
        {
            var personnelIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(personnelIdClaim) || !int.TryParse(personnelIdClaim, out var personnelId))
            {
                return Unauthorized(new { message = "Kullanıcı bilgisi bulunamadı." });
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value ?? "";
            var result = await _dashboardService.GetMyEvaluationsAsync(personnelId, role, year, month);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading my evaluations");
            return StatusCode(500, CreateErrorResponse("Değerlendirmeler yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Kullanıcı tipini döndürür (dashboard view'da hangi paneli göstereceğimizi belirlemek için)
    /// </summary>
    [HttpGet("user-type")]
    public IActionResult GetUserType()
    {
        var userType = User.FindFirst("UserType")?.Value;
        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        var isCustomerPersonnel = userType == "CustomerPersonnel" ||
            role == "CustomerManager" || role == "CustomerSupervisor" ||
            role == "CustomerOperator";

        return Ok(new
        {
            UserType = userType ?? "User",
            Role = role,
            IsCustomerPersonnel = isCustomerPersonnel
        });
    }

    /// <summary>
    /// Kullanıcının bu ayki proje bazlı değerlendirme detayını getirir
    /// </summary>
    [HttpGet("user-projects/{userId}")]
    public async Task<IActionResult> GetUserProjectBreakdown(int userId)
    {
        try
        {
            var breakdown = await _dashboardService.GetUserProjectBreakdownAsync(userId);
            return Ok(breakdown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user project breakdown for user {UserId}", userId);
            return StatusCode(500, CreateErrorResponse("Kullanıcı proje detayı yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Kullanıcının bugünkü proje bazlı değerlendirme detayını getirir
    /// </summary>
    [HttpGet("user-projects-today/{userId}")]
    public async Task<IActionResult> GetUserProjectBreakdownToday(int userId)
    {
        try
        {
            var breakdown = await _dashboardService.GetUserProjectBreakdownTodayAsync(userId);
            return Ok(breakdown);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user project breakdown (today) for user {UserId}", userId);
            return StatusCode(500, CreateErrorResponse("Kullanıcı bugünkü proje detayı yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Firma bazlı aylık trend verilerini getirir
    /// </summary>
    [HttpGet("customer-trend")]
    public async Task<IActionResult> GetCustomerMonthlyTrend()
    {
        try
        {
            var trends = await _dashboardService.GetCustomerMonthlyTrendAsync();
            return Ok(trends);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer monthly trend");
            return StatusCode(500, CreateErrorResponse("Firma trend verileri yüklenirken hata oluştu", ex));
        }
    }
}
