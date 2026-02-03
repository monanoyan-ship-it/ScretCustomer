using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
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
    private readonly ApplicationDbContext _context;

    public DashboardApiController(
        IDashboardService dashboardService,
        ILogger<DashboardApiController> logger,
        ILocalizationService localizationService,
        ApplicationDbContext context,
        IConfiguration configuration) : base(configuration)
    {
        _dashboardService = dashboardService;
        _logger = logger;
        _localizationService = localizationService;
        _context = context;
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
    [Authorize(Roles = "Admin,QualitySpecialist")]
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
            // Token'dan personnelId al
            var personnelIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(personnelIdClaim) || !int.TryParse(personnelIdClaim, out var personnelId))
            {
                return Unauthorized(new { message = "Kullanıcı bilgisi bulunamadı." });
            }

            var role = User.FindFirst(ClaimTypes.Role)?.Value;

            // Varsayılan: mevcut yıl ve ay
            var targetYear = year ?? DateTime.UtcNow.Year;
            var targetMonth = month ?? DateTime.UtcNow.Month;

            // Hedef kullanıcı ID'lerini belirle (role göre)
            var targetUserIds = new List<int>();
            var showPersonnelName = false;

            if (role == "CustomerManager")
            {
                // Manager: Tüm müşteri personelinin değerlendirmeleri
                var personnel = await _context.CustomerPersonnel.FindAsync(personnelId);
                if (personnel != null)
                {
                    targetUserIds = await _context.CustomerPersonnel
                        .Where(p => p.CustomerId == personnel.CustomerId && !p.IsDeleted)
                        .Select(p => p.Id)
                        .ToListAsync();
                }
                showPersonnelName = true;
            }
            else if (role == "CustomerSupervisor")
            {
                // Junction table'dan bu personelin supervisor olduğu kişileri say
                var teamMemberCount = await _context.CustomerPersonnelOrganizations
                    .CountAsync(cpo => cpo.SupervisorId == personnelId && !cpo.IsDeleted);

                if (teamMemberCount == 0)
                {
                    // Organizasyon yöneticisi - tüm organizasyon personelini görsün (sadece junction table)
                    var orgIds = await _context.CustomerPersonnelOrganizations
                        .Where(cpo => cpo.CustomerPersonnelId == personnelId && !cpo.IsDeleted)
                        .Select(cpo => cpo.CustomerOrganizationId)
                        .ToListAsync();

                    if (orgIds.Any())
                    {
                        targetUserIds = await _context.CustomerPersonnelOrganizations
                            .Where(cpo => orgIds.Contains(cpo.CustomerOrganizationId) && !cpo.IsDeleted)
                            .Select(cpo => cpo.CustomerPersonnelId)
                            .Distinct()
                            .ToListAsync();
                    }
                }
                else
                {
                    // Normal supervisor - junction table'dan takım üyeleri
                    targetUserIds = await _context.CustomerPersonnelOrganizations
                        .Where(cpo => cpo.SupervisorId == personnelId && !cpo.IsDeleted)
                        .Select(cpo => cpo.CustomerPersonnelId)
                        .Distinct()
                        .ToListAsync();
                }
                showPersonnelName = true;
            }
            else
            {
                // Operator: Sadece kendi değerlendirmeleri
                targetUserIds.Add(personnelId);
            }

            if (!targetUserIds.Any())
            {
                return Ok(new
                {
                    Year = targetYear,
                    Month = targetMonth,
                    MonthName = new DateTime(targetYear, targetMonth, 1).ToString("MMMM", new System.Globalization.CultureInfo("tr-TR")),
                    TotalCount = 0,
                    AverageScore = 0.0,
                    Evaluations = new List<object>(),
                    ShowPersonnelName = showPersonnelName
                });
            }

            // Değerlendirmeleri getir (junction table ile)
            var evaluations = await _context.Evaluations
                .Include(e => e.Assignment)
                    .ThenInclude(a => a.AssignedCustomerPersonnel)
                        .ThenInclude(cp => cp!.OrganizationAssignments)
                            .ThenInclude(oa => oa.CustomerOrganization)
                .Include(e => e.Assignment)
                    .ThenInclude(a => a.AssignedCustomerPersonnel)
                        .ThenInclude(cp => cp!.OrganizationAssignments)
                            .ThenInclude(oa => oa.Supervisor)
                .Where(e => e.Assignment != null
                    && e.Assignment.AssignedCustomerPersonnelId != null
                    && targetUserIds.Contains(e.Assignment.AssignedCustomerPersonnelId.Value)
                    && e.CreatedAt.Year == targetYear
                    && e.CreatedAt.Month == targetMonth
                    && e.StatusId == EvaluationStatuses.Ids.Completed)
                .OrderByDescending(e => e.CallDate ?? e.CreatedAt)
                .Select(e => new
                {
                    e.Id,
                    CallDate = e.CallDate ?? e.CreatedAt,
                    Score = e.ScorePercentage ?? 0,
                    PersonnelName = e.Assignment!.AssignedCustomerPersonnel != null
                        ? e.Assignment.AssignedCustomerPersonnel.FirstName + " " + e.Assignment.AssignedCustomerPersonnel.LastName
                        : "",
                    // Junction table'dan ilk atamadaki supervisor
                    SupervisorName = e.Assignment.AssignedCustomerPersonnel != null
                        && e.Assignment.AssignedCustomerPersonnel.OrganizationAssignments.Any(oa => !oa.IsDeleted && oa.Supervisor != null)
                        ? e.Assignment.AssignedCustomerPersonnel.OrganizationAssignments
                            .Where(oa => !oa.IsDeleted && oa.Supervisor != null)
                            .Select(oa => oa.Supervisor!.FirstName + " " + oa.Supervisor.LastName)
                            .FirstOrDefault() ?? ""
                        : "",
                    // Junction table'dan ilk atamadaki organizasyon
                    OrganizationName = e.Assignment.AssignedCustomerPersonnel != null
                        && e.Assignment.AssignedCustomerPersonnel.OrganizationAssignments.Any(oa => !oa.IsDeleted && oa.CustomerOrganization != null)
                        ? e.Assignment.AssignedCustomerPersonnel.OrganizationAssignments
                            .Where(oa => !oa.IsDeleted && oa.CustomerOrganization != null)
                            .Select(oa => oa.CustomerOrganization!.Name)
                            .FirstOrDefault() ?? ""
                        : "",
                    e.Notes
                })
                .ToListAsync();

            var isManager = role == "CustomerManager";

            // Manager için filtre listeleri
            List<object>? organizationList = null;
            List<object>? supervisorList = null;
            if (isManager)
            {
                var personnel = await _context.CustomerPersonnel.FindAsync(personnelId);
                if (personnel != null)
                {
                    // Organizasyon listesi
                    organizationList = await _context.CustomerOrganizations
                        .Where(o => o.CustomerId == personnel.CustomerId && !o.IsDeleted)
                        .OrderBy(o => o.Name)
                        .Select(o => new { o.Id, o.Name })
                        .ToListAsync<object>();

                    // Supervisor listesi
                    supervisorList = await _context.CustomerPersonnel
                        .Where(p => p.CustomerId == personnel.CustomerId && !p.IsDeleted && p.RoleId == CustomerPersonnelRoles.Ids.Supervisor)
                        .OrderBy(p => p.FirstName).ThenBy(p => p.LastName)
                        .Select(p => new { p.Id, Name = p.FirstName + " " + p.LastName })
                        .ToListAsync<object>();
                }
            }

            // Personel bazlı özet (Manager/Supervisor için)
            var personnelSummary = showPersonnelName
                ? evaluations
                    .GroupBy(e => new { e.PersonnelName, e.SupervisorName, e.OrganizationName })
                    .Select(g => new
                    {
                        PersonnelName = g.Key.PersonnelName,
                        SupervisorName = g.Key.SupervisorName,
                        OrganizationName = g.Key.OrganizationName,
                        EvaluationCount = g.Count(),
                        AverageScore = Math.Round(g.Average(e => (double)e.Score), 1),
                        MinScore = g.Min(e => e.Score),
                        MaxScore = g.Max(e => e.Score)
                    })
                    .OrderByDescending(p => p.EvaluationCount)
                    .ToList()
                : null;

            // Supervisor bazlı özet (sadece Manager için)
            var supervisorSummary = isManager && evaluations.Any()
                ? evaluations
                    .Where(e => !string.IsNullOrEmpty(e.SupervisorName))
                    .GroupBy(e => e.SupervisorName)
                    .Select(g => new
                    {
                        SupervisorName = g.Key,
                        PersonnelCount = g.Select(e => e.PersonnelName).Distinct().Count(),
                        EvaluationCount = g.Count(),
                        AverageScore = Math.Round(g.Average(e => (double)e.Score), 1)
                    })
                    .OrderByDescending(s => s.EvaluationCount)
                    .ToList()
                : null;

            // Özet hesapla
            var summary = new
            {
                Year = targetYear,
                Month = targetMonth,
                MonthName = new DateTime(targetYear, targetMonth, 1).ToString("MMMM", new System.Globalization.CultureInfo("tr-TR")),
                TotalCount = evaluations.Count,
                AverageScore = evaluations.Any() ? Math.Round(evaluations.Average(e => (double)e.Score), 1) : 0,
                Evaluations = evaluations,
                ShowPersonnelName = showPersonnelName,
                IsManager = isManager,
                PersonnelSummary = personnelSummary,
                SupervisorSummary = supervisorSummary,
                Organizations = organizationList,
                Supervisors = supervisorList
            };

            return Ok(summary);
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
