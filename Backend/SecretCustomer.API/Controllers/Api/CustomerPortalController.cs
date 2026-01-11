using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Core.Enums;
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

    private int? GetPersonnelId()
    {
        var personnelIdClaim = User.FindFirst("PersonnelId")?.Value;
        if (int.TryParse(personnelIdClaim, out var personnelId))
            return personnelId;

        // Token'dan manuel parse et
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            try
            {
                var token = authHeader.Substring("Bearer ".Length);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                var claim = jwtToken.Claims.FirstOrDefault(c => c.Type == "PersonnelId")?.Value;
                if (int.TryParse(claim, out personnelId))
                    return personnelId;
            }
            catch { }
        }
        return null;
    }

    private string? GetPersonnelRole()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.IsNullOrEmpty(roleClaim))
            return roleClaim;

        // Token'dan manuel parse et
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            try
            {
                var token = authHeader.Substring("Bearer ".Length);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value;
            }
            catch { }
        }
        return null;
    }

    /// <summary>
    /// Süpervizör ise takımındaki personel ID'lerini döndürür, değilse null (tüm personel görülebilir)
    /// </summary>
    private async Task<List<int>?> GetAllowedPersonnelIdsAsync()
    {
        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();

        // CustomerManager tüm personeli görebilir
        if (role == "CustomerManager")
            return null;

        // CustomerSupervisor
        if (role == "CustomerSupervisor" && personnelId.HasValue)
        {
            // Önce süpervizör olarak atandığı personelleri bul
            var teamMemberIds = await _context.CustomerPersonnelOrganizations
                .Where(cpo => cpo.SupervisorId == personnelId.Value)
                .Select(cpo => cpo.CustomerPersonnelId)
                .Distinct()
                .ToListAsync();

            // Eğer altında eleman varsa, onları göster
            if (teamMemberIds.Any())
            {
                // Kendisini de ekle
                if (!teamMemberIds.Contains(personnelId.Value))
                    teamMemberIds.Add(personnelId.Value);

                return teamMemberIds;
            }

            // Altında eleman yoksa, atandığı organizasyonlardaki tüm personeli göster
            var myOrganizationIds = await _context.CustomerPersonnelOrganizations
                .Where(cpo => cpo.CustomerPersonnelId == personnelId.Value)
                .Select(cpo => cpo.CustomerOrganizationId)
                .ToListAsync();

            if (myOrganizationIds.Any())
            {
                var orgPersonnelIds = await _context.CustomerPersonnelOrganizations
                    .Where(cpo => myOrganizationIds.Contains(cpo.CustomerOrganizationId))
                    .Select(cpo => cpo.CustomerPersonnelId)
                    .Distinct()
                    .ToListAsync();

                // Kendisini de ekle
                if (!orgPersonnelIds.Contains(personnelId.Value))
                    orgPersonnelIds.Add(personnelId.Value);

                return orgPersonnelIds;
            }

            // Hiçbir organizasyona atanmamışsa sadece kendini görebilir
            return new List<int> { personnelId.Value };
        }

        // CustomerOperator sadece kendini görebilir
        if (personnelId.HasValue)
            return new List<int> { personnelId.Value };

        return new List<int>(); // Hiçbir şey göremez
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

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        // Organizasyon sayısı (Supervisor için kendi organizasyonları)
        int organizationCount;
        if (allowedPersonnelIds == null)
        {
            // Manager - tüm organizasyonlar
            organizationCount = await _context.CustomerOrganizations
                .CountAsync(o => o.CustomerId == customerId && !o.IsDeleted && o.IsActive);
        }
        else
        {
            // Supervisor/Operator - sadece bağlı olduğu organizasyonlar
            organizationCount = await _context.CustomerPersonnelOrganizations
                .Where(cpo => allowedPersonnelIds.Contains(cpo.CustomerPersonnelId))
                .Select(cpo => cpo.CustomerOrganizationId)
                .Distinct()
                .CountAsync();
        }

        // Değerlendirmeler - rol bazlı filtreleme
        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null &&
                        e.Assignment.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed);

        // Supervisor/Operator için personel filtresi
        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        var evaluations = await evaluationsQuery.ToListAsync();

        var totalEvaluations = evaluations.Count;
        var averageScore = evaluations.Any() ? evaluations.Average(e => e.ScorePercentage ?? 0) : 0;

        var thisMonth = DateTime.UtcNow.Month;
        var thisYear = DateTime.UtcNow.Year;
        var thisMonthEvaluations = evaluations.Count(e =>
            e.CreatedAt.Month == thisMonth && e.CreatedAt.Year == thisYear);

        return Ok(new
        {
            organizationCount,
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

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);

        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null &&
                        e.Assignment.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.CreatedAt >= startDate);

        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        var evaluations = await evaluationsQuery.ToListAsync();

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

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null &&
                        e.Assignment.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.ScorePercentage.HasValue);

        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        var scores = await evaluationsQuery.Select(e => e.ScorePercentage!.Value).ToListAsync();

        var distribution = new
        {
            excellent = scores.Count(s => s >= 90),   // Mükemmel (90+)
            good = scores.Count(s => s >= 80 && s < 90),  // İyi (80-89)
            average = scores.Count(s => s >= 60 && s < 80), // Orta (60-79)
            poor = scores.Count(s => s < 60)  // Düşük (<60)
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

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Where(e => e.Assignment != null && e.Assignment.Project != null &&
                        e.Assignment.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed);

        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        var evaluations = await evaluationsQuery
            .OrderByDescending(e => e.CompletedAt ?? e.CreatedAt)
            .Take(count)
            .Select(e => new
            {
                e.Id,
                evaluationDate = e.CompletedAt ?? e.CreatedAt,
                projectName = e.Assignment!.Project!.Name,
                checklistName = e.Assignment.Checklist != null ? e.Assignment.Checklist.Name : "N/A",
                personnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : e.EvaluatedUnknownPersonnel ?? "-",
                score = e.ScorePercentage ?? 0,
                statusId = e.StatusId
            })
            .ToListAsync();

        var result = evaluations.Select(e => new
        {
            e.Id,
            e.evaluationDate,
            branchName = e.projectName,
            e.checklistName,
            e.personnelName,
            e.score,
            status = EvaluationStatuses.GetById(e.statusId)?.SystemName ?? "",
            statusText = GetStatusText(e.statusId)
        });

        return Ok(result);
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
                statusId = e.StatusId
            })
            .ToListAsync();

        var mappedEvaluations = evaluations.Select(e => new
        {
            e.Id,
            e.evaluationDate,
            e.branchName,
            e.checklistName,
            e.score,
            status = EvaluationStatuses.GetById(e.statusId)?.SystemName ?? "",
            statusText = GetStatusText(e.statusId)
        });

        return Ok(new
        {
            items = mappedEvaluations,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    private static string GetStatusText(int statusId)
    {
        return statusId switch
        {
            EvaluationStatuses.Ids.Pending => "Beklemede",
            EvaluationStatuses.Ids.Draft => "Taslak",
            EvaluationStatuses.Ids.InProgress => "Devam Ediyor",
            EvaluationStatuses.Ids.Completed => "Tamamlandı",
            EvaluationStatuses.Ids.Cancelled => "İptal Edildi",
            _ => EvaluationStatuses.GetById(statusId)?.SystemName ?? "Bilinmeyen"
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
                && e.StatusId == EvaluationStatuses.Ids.Completed)
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
                && e.StatusId == EvaluationStatuses.Ids.Completed)
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
                && e.StatusId == EvaluationStatuses.Ids.Completed)
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
