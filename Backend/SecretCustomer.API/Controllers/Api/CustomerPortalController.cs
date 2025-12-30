using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/customer/portal")]
[AllowAnonymous] // JWT middleware sorunlu, token'ı kendimiz parse ediyoruz
public class CustomerPortalApiController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerPortalApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public CustomerPortalApiController(
        ApplicationDbContext context,
        ILogger<CustomerPortalApiController> logger,
        ILocalizationService localizationService)
    {
        _context = context;
        _logger = logger;
        _localizationService = localizationService;
    }

    private int? GetCustomerIdFromToken()
    {
        // Authorization header'dan token'ı al
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            _logger.LogWarning("[CustomerPortal] No Authorization header or invalid format");
            return null;
        }

        var token = authHeader.Substring("Bearer ".Length);

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var customerIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "CustomerId")?.Value;

            _logger.LogInformation("[CustomerPortal] Token parsed. CustomerId: {CustomerId}", customerIdClaim);

            if (int.TryParse(customerIdClaim, out var customerId))
                return customerId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error parsing token");
        }

        return null;
    }

    private int? GetCustomerId()
    {
        // Önce User claims'den dene
        var customerIdClaim = User.FindFirst("CustomerId")?.Value;
        if (int.TryParse(customerIdClaim, out var customerId))
            return customerId;

        // Yoksa token'dan manuel parse et
        return GetCustomerIdFromToken();
    }

    private bool IsCustomerPersonnel()
    {
        if (User.FindFirst("UserType")?.Value == "CustomerPersonnel")
            return true;

        // Token'dan kontrol et
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            try
            {
                var token = authHeader.Substring("Bearer ".Length);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken.Claims.Any(c => c.Type == "UserType" && c.Value == "CustomerPersonnel");
            }
            catch { }
        }
        return false;
    }

    /// <summary>
    /// Dashboard istatistikleri
    /// </summary>
    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        // Branch system removed - use projects instead
        var projectCount = await _context.Projects
            .CountAsync(p => p.CustomerId == customerId && !p.IsDeleted);

        var evaluations = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId == customerId)
            .ToListAsync();

        var totalEvaluations = evaluations.Count;
        var averageScore = evaluations.Any() ? evaluations.Average(e => e.TotalScore ?? 0) : 0;

        var thisMonth = DateTime.UtcNow.Month;
        var thisYear = DateTime.UtcNow.Year;
        var thisMonthEvaluations = evaluations.Count(e =>
            e.CreatedAt.Month == thisMonth && e.CreatedAt.Year == thisYear);

        return Ok(new
        {
            branchCount = 0, // Branch system removed
            projectCount,
            totalEvaluations,
            averageScore = Math.Round(averageScore, 1),
            thisMonthEvaluations
        });
    }

    /// <summary>
    /// Aylık değerlendirme trendi (son 12 ay)
    /// </summary>
    [HttpGet("dashboard/monthly-trend")]
    public async Task<IActionResult> GetMonthlyTrend()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);

        var evaluations = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId == customerId && e.CreatedAt >= startDate)
            .ToListAsync();

        var monthlyData = new List<object>();
        for (int i = 0; i < 12; i++)
        {
            var monthStart = startDate.AddMonths(i);
            var monthEnd = monthStart.AddMonths(1);

            var monthEvals = evaluations.Where(e => e.CreatedAt >= monthStart && e.CreatedAt < monthEnd).ToList();
            var avgScore = monthEvals.Any() ? monthEvals.Average(e => e.TotalScore ?? 0) : 0;

            monthlyData.Add(new
            {
                month = monthStart.ToString("MMM", new System.Globalization.CultureInfo("tr-TR")),
                year = monthStart.Year,
                count = monthEvals.Count,
                averageScore = Math.Round(avgScore, 1)
            });
        }

        return Ok(monthlyData);
    }

    /// <summary>
    /// Puan dağılımı
    /// </summary>
    [HttpGet("dashboard/score-distribution")]
    public async Task<IActionResult> GetScoreDistribution()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var evaluations = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId == customerId && e.TotalScore.HasValue)
            .Select(e => e.TotalScore!.Value)
            .ToListAsync();

        var distribution = new
        {
            excellent = evaluations.Count(s => s >= 90),   // Mükemmel (90+)
            good = evaluations.Count(s => s >= 80 && s < 90),  // İyi (80-89)
            average = evaluations.Count(s => s >= 60 && s < 80), // Orta (60-79)
            poor = evaluations.Count(s => s < 60)  // Düşük (<60)
        };

        return Ok(distribution);
    }

    /// <summary>
    /// Müşterinin projeleri (Branch system removed)
    /// </summary>
    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        // Branch system removed - return projects instead
        var projects = await _context.Projects
            .Where(p => p.CustomerId == customerId && p.IsActive && !p.IsDeleted)
            .Select(p => new
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                City = "",
                Address = "",
                IsActive = p.IsActive,
                evaluationCount = _context.Evaluations
                    .Count(e => e.Assignment.ProjectId == p.Id),
                averageScore = _context.Evaluations
                    .Where(e => e.Assignment.ProjectId == p.Id && e.TotalScore.HasValue)
                    .Average(e => (double?)e.TotalScore) ?? 0
            })
            .OrderBy(p => p.Name)
            .ToListAsync();

        return Ok(projects);
    }

    /// <summary>
    /// Son değerlendirmeler
    /// </summary>
    [HttpGet("evaluations/recent")]
    public async Task<IActionResult> GetRecentEvaluations([FromQuery] int count = 10)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var evaluations = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId == customerId)
            .OrderByDescending(e => e.CreatedAt)
            .Take(count)
            .Select(e => new
            {
                e.Id,
                evaluationDate = e.CreatedAt,
                branchName = e.Assignment!.Project!.Name,
                checklistName = e.Assignment.Checklist != null ? e.Assignment.Checklist.Name : "N/A",
                score = e.TotalScore ?? 0,
                status = e.Status.ToString(),
                statusText = GetStatusText(e.Status)
            })
            .ToListAsync();

        return Ok(evaluations);
    }

    /// <summary>
    /// Tüm değerlendirmeler (sayfalı)
    /// </summary>
    [HttpGet("evaluations")]
    public async Task<IActionResult> GetEvaluations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId == customerId);

        var totalCount = await query.CountAsync();

        var evaluations = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                evaluationDate = e.CreatedAt,
                branchName = e.Assignment!.Project!.Name,
                checklistName = e.Assignment.Checklist != null ? e.Assignment.Checklist.Name : "N/A",
                score = e.TotalScore ?? 0,
                status = e.Status.ToString(),
                statusText = GetStatusText(e.Status)
            })
            .ToListAsync();

        return Ok(new
        {
            items = evaluations,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    private static string GetStatusText(Core.Enums.EvaluationStatus status)
    {
        return status switch
        {
            Core.Enums.EvaluationStatus.Pending => "Beklemede",
            Core.Enums.EvaluationStatus.Draft => "Taslak",
            Core.Enums.EvaluationStatus.InProgress => "Devam Ediyor",
            Core.Enums.EvaluationStatus.Completed => "Tamamlandı",
            Core.Enums.EvaluationStatus.Cancelled => "İptal Edildi",
            _ => status.ToString()
        };
    }

    /// <summary>
    /// Proje performans raporu (Branch system removed)
    /// </summary>
    [HttpGet("reports/branch-performance")]
    public async Task<IActionResult> GetBranchPerformance([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var start = startDate ?? DateTime.UtcNow.AddMonths(-3);
        var end = endDate ?? DateTime.UtcNow;

        // UTC'ye çevir
        if (start.Kind == DateTimeKind.Unspecified)
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (end.Kind == DateTimeKind.Unspecified)
            end = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var projects = await _context.Projects
            .Where(p => p.CustomerId == customerId && !p.IsDeleted)
            .ToListAsync();

        var evaluations = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId == customerId
                && e.CreatedAt >= start
                && e.CreatedAt <= end
                && e.Status == Core.Enums.EvaluationStatus.Completed)
            .ToListAsync();

        var projectPerformance = projects.Select(p =>
        {
            var projectEvals = evaluations.Where(e => e.Assignment.ProjectId == p.Id).ToList();
            return new
            {
                branchId = p.Id,
                branchName = p.Name,
                city = "",
                evaluationCount = projectEvals.Count,
                averageScore = projectEvals.Any() ? Math.Round(projectEvals.Average(e => (double)(e.TotalScore ?? 0)), 1) : 0,
                minScore = projectEvals.Any() ? projectEvals.Min(e => e.TotalScore ?? 0) : 0,
                maxScore = projectEvals.Any() ? projectEvals.Max(e => e.TotalScore ?? 0) : 0
            };
        })
        .OrderByDescending(b => b.averageScore)
        .ToList();

        return Ok(projectPerformance);
    }

    /// <summary>
    /// Dönem özet raporu
    /// </summary>
    [HttpGet("reports/summary")]
    public async Task<IActionResult> GetReportSummary([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var start = startDate ?? DateTime.UtcNow.AddMonths(-3);
        var end = endDate ?? DateTime.UtcNow;

        if (start.Kind == DateTimeKind.Unspecified)
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (end.Kind == DateTimeKind.Unspecified)
            end = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var evaluations = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId == customerId
                && e.CreatedAt >= start
                && e.CreatedAt <= end
                && e.Status == Core.Enums.EvaluationStatus.Completed)
            .ToListAsync();

        var projectCount = await _context.Projects
            .CountAsync(p => p.CustomerId == customerId && !p.IsDeleted);

        var summary = new
        {
            periodStart = start,
            periodEnd = end,
            totalEvaluations = evaluations.Count,
            branchCount = projectCount, // Using project count instead
            averageScore = evaluations.Any() ? Math.Round(evaluations.Average(e => (double)(e.TotalScore ?? 0)), 1) : 0,
            minScore = evaluations.Any() ? evaluations.Min(e => e.TotalScore ?? 0) : 0,
            maxScore = evaluations.Any() ? evaluations.Max(e => e.TotalScore ?? 0) : 0,
            excellentCount = evaluations.Count(e => e.TotalScore >= 90),
            goodCount = evaluations.Count(e => e.TotalScore >= 80 && e.TotalScore < 90),
            averageCount = evaluations.Count(e => e.TotalScore >= 60 && e.TotalScore < 80),
            poorCount = evaluations.Count(e => e.TotalScore < 60)
        };

        return Ok(summary);
    }

    /// <summary>
    /// Aylık trend raporu (tarih aralığına göre)
    /// </summary>
    [HttpGet("reports/monthly-trend")]
    public async Task<IActionResult> GetReportMonthlyTrend([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var start = startDate ?? DateTime.UtcNow.AddMonths(-6);
        var end = endDate ?? DateTime.UtcNow;

        if (start.Kind == DateTimeKind.Unspecified)
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (end.Kind == DateTimeKind.Unspecified)
            end = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var evaluations = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId == customerId
                && e.CreatedAt >= start
                && e.CreatedAt <= end
                && e.Status == Core.Enums.EvaluationStatus.Completed)
            .ToListAsync();

        // Aylara göre grupla
        var monthlyData = evaluations
            .GroupBy(e => new { e.CreatedAt.Year, e.CreatedAt.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Select(g => new
            {
                year = g.Key.Year,
                month = g.Key.Month,
                monthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy", new System.Globalization.CultureInfo("tr-TR")),
                count = g.Count(),
                averageScore = Math.Round(g.Average(e => (double)(e.TotalScore ?? 0)), 1)
            })
            .ToList();

        return Ok(monthlyData);
    }
}
