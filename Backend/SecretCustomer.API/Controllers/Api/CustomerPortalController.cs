using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Core.DTOs.Report;
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
    private readonly IReportService _reportService;

    public CustomerPortalApiController(
        ApplicationDbContext context,
        ILogger<CustomerPortalApiController> logger,
        ILocalizationService localizationService,
        IReportService reportService)
    {
        _context = context;
        _logger = logger;
        _localizationService = localizationService;
        _reportService = reportService;
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
            var withScore = monthEvals.Where(e => e.ScorePercentage.HasValue).ToList();
            var avgScore = withScore.Any() ? withScore.Average(e => (double)e.ScorePercentage!.Value) : 0;

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
    /// Müşterinin projeleri
    /// </summary>
    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });
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
                    .Where(e => e.Assignment.ProjectId == p.Id && e.ScorePercentage.HasValue)
                    .Average(e => (double?)e.ScorePercentage) ?? 0
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
            e.projectName,
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
                projectName = e.Assignment!.Project!.Name,
                checklistName = e.Assignment.Checklist != null ? e.Assignment.Checklist.Name : "N/A",
                score = e.ScorePercentage ?? 0,
                statusId = e.StatusId
            })
            .ToListAsync();

        var mappedEvaluations = evaluations.Select(e => new
        {
            e.Id,
            e.evaluationDate,
            e.projectName,
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
    /// Proje performans raporu
    /// </summary>
    [HttpGet("reports/project-performance")]
    public async Task<IActionResult> GetProjectPerformance([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
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
                projectId = p.Id,
                projectName = p.Name,
                evaluationCount = projectEvals.Count,
                averageScore = projectEvals.Any() ? Math.Round(projectEvals.Where(e => e.ScorePercentage.HasValue).Average(e => (double)e.ScorePercentage!.Value), 1) : 0,
                minScore = projectEvals.Where(e => e.ScorePercentage.HasValue).Any() ? projectEvals.Where(e => e.ScorePercentage.HasValue).Min(e => e.ScorePercentage!.Value) : 0,
                maxScore = projectEvals.Where(e => e.ScorePercentage.HasValue).Any() ? projectEvals.Where(e => e.ScorePercentage.HasValue).Max(e => e.ScorePercentage!.Value) : 0
            };
        })
        .OrderByDescending(p => p.averageScore)
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
            projectCount,
            averageScore = evaluations.Where(e => e.ScorePercentage.HasValue).Any() ? Math.Round(evaluations.Where(e => e.ScorePercentage.HasValue).Average(e => (double)e.ScorePercentage!.Value), 1) : 0,
            minScore = evaluations.Where(e => e.ScorePercentage.HasValue).Any() ? evaluations.Where(e => e.ScorePercentage.HasValue).Min(e => e.ScorePercentage!.Value) : 0,
            maxScore = evaluations.Where(e => e.ScorePercentage.HasValue).Any() ? evaluations.Where(e => e.ScorePercentage.HasValue).Max(e => e.ScorePercentage!.Value) : 0,
            excellentCount = evaluations.Count(e => e.ScorePercentage >= 90),
            goodCount = evaluations.Count(e => e.ScorePercentage >= 80 && e.ScorePercentage < 90),
            averageCount = evaluations.Count(e => e.ScorePercentage >= 60 && e.ScorePercentage < 80),
            poorCount = evaluations.Count(e => e.ScorePercentage < 60)
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
                averageScore = g.Where(e => e.ScorePercentage.HasValue).Any() ? Math.Round(g.Where(e => e.ScorePercentage.HasValue).Average(e => (double)e.ScorePercentage!.Value), 1) : 0
            })
            .ToList();

        return Ok(monthlyData);
    }

    /// <summary>
    /// Organizasyonlar (gruplu - hiyerarşik)
    /// </summary>
    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var organizations = await _context.CustomerOrganizations
            .Where(o => o.CustomerId == customerId && o.IsActive && !o.IsDeleted)
            .OrderBy(o => o.ParentId)
            .ThenBy(o => o.Order)
            .ThenBy(o => o.Name)
            .Select(o => new
            {
                o.Id,
                o.Name,
                o.Code,
                o.Description,
                o.ParentId,
                parentName = o.Parent != null ? o.Parent.Name : null,
                o.Level,
                o.Order,
                personnelCount = _context.CustomerPersonnelOrganizations
                    .Count(cpo => cpo.CustomerOrganizationId == o.Id &&
                                  !cpo.CustomerPersonnel.IsDeleted &&
                                  cpo.CustomerPersonnel.IsActive),
                evaluationCount = _context.Evaluations
                    .Count(e => e.EvaluatedOrganizationId == o.Id &&
                               e.StatusId == EvaluationStatuses.Ids.Completed),
                averageScore = _context.Evaluations
                    .Where(e => e.EvaluatedOrganizationId == o.Id &&
                               e.StatusId == EvaluationStatuses.Ids.Completed &&
                               e.ScorePercentage.HasValue)
                    .Average(e => (double?)e.ScorePercentage) ?? 0
            })
            .ToListAsync();

        // Group by parent (null parent = independent/root level)
        var grouped = organizations
            .GroupBy(o => o.parentName ?? "Bağımsız")
            .Select(g => new
            {
                groupName = g.Key,
                organizations = g.ToList()
            })
            .OrderBy(g => g.groupName == "Bağımsız" ? "" : g.groupName) // Bağımsız en başa
            .ToList();

        return Ok(grouped);
    }

    /// <summary>
    /// Süpervizörler (gruplu - organizasyona göre)
    /// </summary>
    [HttpGet("supervisors")]
    public async Task<IActionResult> GetSupervisors()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        // Süpervizör olan personelleri bul (CustomerPersonnelOrganization'da SupervisorId olarak geçenler)
        var supervisorIds = await _context.CustomerPersonnelOrganizations
            .Where(cpo => cpo.SupervisorId.HasValue &&
                          cpo.CustomerOrganization.CustomerId == customerId)
            .Select(cpo => cpo.SupervisorId!.Value)
            .Distinct()
            .ToListAsync();

        var supervisors = await _context.CustomerPersonnel
            .Where(cp => supervisorIds.Contains(cp.Id) && cp.IsActive && !cp.IsDeleted)
            .OrderBy(cp => cp.FirstName).ThenBy(cp => cp.LastName)
            .Select(cp => new
            {
                cp.Id,
                fullName = cp.FirstName + " " + cp.LastName,
                cp.Email,
                cp.Title,
                organizations = _context.CustomerPersonnelOrganizations
                    .Where(cpo => cpo.SupervisorId == cp.Id)
                    .Select(cpo => new { cpo.CustomerOrganization.Id, cpo.CustomerOrganization.Name })
                    .Distinct()
                    .ToList(),
                personnelCount = _context.CustomerPersonnelOrganizations
                    .Count(cpo => cpo.SupervisorId == cp.Id),
                evaluationCount = _context.Evaluations
                    .Count(e => e.EvaluatorCustomerPersonnelId == cp.Id &&
                               e.StatusId == EvaluationStatuses.Ids.Completed),
                averageScore = _context.Evaluations
                    .Where(e => e.EvaluatorCustomerPersonnelId == cp.Id &&
                               e.StatusId == EvaluationStatuses.Ids.Completed &&
                               e.ScorePercentage.HasValue)
                    .Average(e => (double?)e.ScorePercentage) ?? 0
            })
            .ToListAsync();

        // Group by first organization
        var grouped = supervisors
            .GroupBy(s => s.organizations.FirstOrDefault()?.Name ?? "Atanmamış")
            .Select(g => new
            {
                groupName = g.Key,
                supervisors = g.ToList()
            })
            .OrderBy(g => g.groupName == "Atanmamış" ? "ZZZZ" : g.groupName)
            .ToList();

        return Ok(grouped);
    }

    /// <summary>
    /// İç dinlemeler (firma personeli tarafından yapılan)
    /// </summary>
    [HttpGet("evaluations/internal")]
    public async Task<IActionResult> GetInternalEvaluations(
        [FromQuery] int? page = 1,
        [FromQuery] int? pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? projectId = null,
        [FromQuery] string? evaluatorName = null,
        [FromQuery] string? personnelName = null,
        [FromQuery] int? organizationId = null,
        [FromQuery] string? callId = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.EvaluatorCustomerPersonnel)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Where(e => e.Assignment.Project.CustomerId == customerId &&
                       e.EvaluatorCustomerPersonnelId != null &&
                       e.StatusId == EvaluationStatuses.Ids.Completed);

        // Date filters
        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => (e.CallDate ?? e.CompletedAt ?? e.CreatedAt) >= start);
        }
        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            query = query.Where(e => (e.CallDate ?? e.CompletedAt ?? e.CreatedAt) <= end);
        }

        // Project filter
        if (projectId.HasValue)
        {
            query = query.Where(e => e.Assignment.ProjectId == projectId.Value);
        }

        // Evaluator name filter
        if (!string.IsNullOrWhiteSpace(evaluatorName))
        {
            var evalLower = evaluatorName.ToLower();
            query = query.Where(e => e.EvaluatorCustomerPersonnel != null &&
                (e.EvaluatorCustomerPersonnel.FirstName + " " + e.EvaluatorCustomerPersonnel.LastName).ToLower().Contains(evalLower));
        }

        // Personnel name filter
        if (!string.IsNullOrWhiteSpace(personnelName))
        {
            var persLower = personnelName.ToLower();
            query = query.Where(e =>
                (e.EvaluatedCustomerPersonnel != null && (e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName).ToLower().Contains(persLower)) ||
                (e.EvaluatedUnknownPersonnel != null && e.EvaluatedUnknownPersonnel.ToLower().Contains(persLower)));
        }

        // Organization filter
        if (organizationId.HasValue)
        {
            query = query.Where(e => e.EvaluatedOrganizationId == organizationId.Value);
        }

        // CallId filter
        if (!string.IsNullOrWhiteSpace(callId))
        {
            var callIdLower = callId.ToLower();
            query = query.Where(e => e.CallId != null && e.CallId.ToLower().Contains(callIdLower));
        }

        // General search filter (legacy support)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(e =>
                (e.Assignment.Project.Name != null && e.Assignment.Project.Name.ToLower().Contains(searchLower)) ||
                (e.EvaluatorCustomerPersonnel != null && (e.EvaluatorCustomerPersonnel.FirstName + " " + e.EvaluatorCustomerPersonnel.LastName).ToLower().Contains(searchLower)) ||
                (e.EvaluatedCustomerPersonnel != null && (e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName).ToLower().Contains(searchLower)) ||
                (e.EvaluatedUnknownPersonnel != null && e.EvaluatedUnknownPersonnel.ToLower().Contains(searchLower)) ||
                (e.EvaluatedOrganization != null && e.EvaluatedOrganization.Name.ToLower().Contains(searchLower)) ||
                (e.CallId != null && e.CallId.ToLower().Contains(searchLower)));
        }

        var total = await query.CountAsync();
        var averageScore = await query.Where(e => e.ScorePercentage.HasValue).AverageAsync(e => (double?)e.ScorePercentage) ?? 0;

        var evaluations = await query
            .OrderByDescending(e => e.CallDate ?? e.CompletedAt ?? e.CreatedAt)
            .Skip(((page ?? 1) - 1) * (pageSize ?? 20))
            .Take(pageSize ?? 20)
            .Select(e => new
            {
                e.Id,
                evaluationDate = e.CompletedAt ?? e.CreatedAt,
                projectName = e.Assignment.Project.Name,
                evaluatorName = e.EvaluatorCustomerPersonnel != null ? e.EvaluatorCustomerPersonnel.FirstName + " " + e.EvaluatorCustomerPersonnel.LastName : null,
                evaluatedPersonnelName = e.EvaluatedCustomerPersonnel != null ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName : e.EvaluatedUnknownPersonnel,
                organizationName = e.EvaluatedOrganization != null ? e.EvaluatedOrganization.Name : null,
                e.TotalScore,
                e.ScorePercentage,
                e.YellowCardCount,
                e.RedCardCount,
                e.CallId,
                e.CallDate,
                e.CallTime,
                e.Duration
            })
            .ToListAsync();

        return Ok(new { items = evaluations, total, page = page ?? 1, pageSize = pageSize ?? 20, averageScore = Math.Round(averageScore, 1) });
    }

    #region Saved Filters

    /// <summary>
    /// CustomerPortal - Kayıtlı filtreleri getir (aynı customer'ın tüm personelleri görür)
    /// </summary>
    [HttpGet("saved-filters")]
    public async Task<IActionResult> GetSavedFilters([FromQuery] string page)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = "Müşteri bilgisi alınamadı" });

        var savedFilters = await _context.SavedFilters
            .Where(f => f.CustomerId == customerId.Value && f.PageName == page && !f.IsDeleted)
            .OrderByDescending(f => f.IsDefault)
            .ThenByDescending(f => f.CreatedAt)
            .Select(f => new
            {
                f.Id,
                f.Name,
                f.PageName,
                f.FilterData,
                f.IsDefault,
                f.CreatedAt
            })
            .ToListAsync();

        // Parse FilterData outside of LINQ expression
        var result = savedFilters.Select(f => new
        {
            f.Id,
            f.Name,
            f.PageName,
            f.IsDefault,
            f.CreatedAt,
            filters = System.Text.Json.JsonSerializer.Deserialize<List<object>>(f.FilterData)
        });

        return Ok(result);
    }

    /// <summary>
    /// CustomerPortal - Filtre kaydet (CustomerId ile - tüm personeller görebilir)
    /// </summary>
    [HttpPost("saved-filters")]
    public async Task<IActionResult> SaveFilter([FromBody] CustomerSaveFilterRequest request)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = "Müşteri bilgisi alınamadı" });

        if (string.IsNullOrEmpty(request.Name))
            return BadRequest(new { message = "Filtre adı zorunludur" });

        var filter = new Core.Entities.SavedFilter
        {
            CustomerId = customerId.Value,
            UserId = null, // CustomerPortal'dan kaydedildi
            PageName = request.Page,
            Name = request.Name,
            FilterData = System.Text.Json.JsonSerializer.Serialize(request.Filters),
            IsDefault = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.SavedFilters.Add(filter);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Filtre kaydedildi", id = filter.Id });
    }

    /// <summary>
    /// CustomerPortal - Filtre sil (sadece kendi customer'ının filtresini silebilir)
    /// </summary>
    [HttpDelete("saved-filters/{id}")]
    public async Task<IActionResult> DeleteSavedFilter(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = "Müşteri bilgisi alınamadı" });

        var filter = await _context.SavedFilters
            .FirstOrDefaultAsync(f => f.Id == id && f.CustomerId == customerId.Value);

        if (filter == null)
            return NotFound(new { message = "Filtre bulunamadı" });

        filter.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Filtre silindi" });
    }

    public class CustomerSaveFilterRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Page { get; set; } = string.Empty;
        public List<object> Filters { get; set; } = new();
    }

    #endregion

    /// <summary>
    /// Dış dinlemeler (bizim tarafımızdan yapılan)
    /// </summary>
    [HttpGet("evaluations/external")]
    public async Task<IActionResult> GetExternalEvaluations(
        [FromQuery] int? page = 1,
        [FromQuery] int? pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] int? projectId = null,
        [FromQuery] string? personnelName = null,
        [FromQuery] int? organizationId = null,
        [FromQuery] string? callId = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Evaluator)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Where(e => e.Assignment.Project.CustomerId == customerId &&
                       e.EvaluatorId != null &&
                       e.StatusId == EvaluationStatuses.Ids.Completed);

        // Date filters
        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => (e.CallDate ?? e.CompletedAt ?? e.CreatedAt) >= start);
        }
        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            query = query.Where(e => (e.CallDate ?? e.CompletedAt ?? e.CreatedAt) <= end);
        }

        // Project filter
        if (projectId.HasValue)
        {
            query = query.Where(e => e.Assignment.ProjectId == projectId.Value);
        }

        // Personnel name filter
        if (!string.IsNullOrWhiteSpace(personnelName))
        {
            var persLower = personnelName.ToLower();
            query = query.Where(e =>
                (e.EvaluatedCustomerPersonnel != null && (e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName).ToLower().Contains(persLower)) ||
                (e.EvaluatedUnknownPersonnel != null && e.EvaluatedUnknownPersonnel.ToLower().Contains(persLower)));
        }

        // Organization filter
        if (organizationId.HasValue)
        {
            query = query.Where(e => e.EvaluatedOrganizationId == organizationId.Value);
        }

        // CallId filter
        if (!string.IsNullOrWhiteSpace(callId))
        {
            var callIdLower = callId.ToLower();
            query = query.Where(e => e.CallId != null && e.CallId.ToLower().Contains(callIdLower));
        }

        // General search filter (legacy support)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(e =>
                (e.Assignment.Project.Name != null && e.Assignment.Project.Name.ToLower().Contains(searchLower)) ||
                (e.EvaluatedCustomerPersonnel != null && (e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName).ToLower().Contains(searchLower)) ||
                (e.EvaluatedUnknownPersonnel != null && e.EvaluatedUnknownPersonnel.ToLower().Contains(searchLower)) ||
                (e.EvaluatedOrganization != null && e.EvaluatedOrganization.Name.ToLower().Contains(searchLower)) ||
                (e.CallId != null && e.CallId.ToLower().Contains(searchLower)));
        }

        var total = await query.CountAsync();
        var averageScore = await query.Where(e => e.ScorePercentage.HasValue).AverageAsync(e => (double?)e.ScorePercentage) ?? 0;

        var evaluations = await query
            .OrderByDescending(e => e.CallDate ?? e.CompletedAt ?? e.CreatedAt)
            .Skip(((page ?? 1) - 1) * (pageSize ?? 20))
            .Take(pageSize ?? 20)
            .Select(e => new
            {
                e.Id,
                evaluationDate = e.CompletedAt ?? e.CreatedAt,
                projectName = e.Assignment.Project.Name,
                evaluatedPersonnelName = e.EvaluatedCustomerPersonnel != null ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName : e.EvaluatedUnknownPersonnel,
                organizationName = e.EvaluatedOrganization != null ? e.EvaluatedOrganization.Name : null,
                e.TotalScore,
                e.ScorePercentage,
                e.YellowCardCount,
                e.RedCardCount,
                e.CallId,
                e.CallDate,
                e.CallTime,
                e.Duration
            })
            .ToListAsync();

        return Ok(new { items = evaluations, total, page = page ?? 1, pageSize = pageSize ?? 20, averageScore = Math.Round(averageScore, 1) });
    }

    /// <summary>
    /// Değerlendirme detayı Excel export (CustomerPortal)
    /// </summary>
    [HttpGet("evaluations/{evaluationId}/export")]
    public async Task<IActionResult> ExportEvaluationDetail(int evaluationId)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Müşteri sadece kendi değerlendirmesini export edebilir
            var evaluation = await _context.Evaluations
                .Include(e => e.Assignment)
                    .ThenInclude(a => a!.Project)
                .FirstOrDefaultAsync(e => e.Id == evaluationId);

            if (evaluation?.Assignment?.Project?.CustomerId != customerId)
                return Forbid();

            var result = await _reportService.ExportEvaluationDetailToExcelAsync(evaluationId);
            if (result == null)
                return NotFound(new { message = "Değerlendirme bulunamadı." });

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting evaluation detail {EvaluationId} for customer {CustomerId}", evaluationId, customerId);
            return StatusCode(500, new { message = "Değerlendirme export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Soru Grubu Ortalama Raporu - Excel export (CustomerPortal)
    /// </summary>
    [HttpPost("reports/export/question-group-average")]
    public async Task<IActionResult> ExportQuestionGroupAverageReport([FromBody] ReportFilterDto filter)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Müşteri sadece kendi projesinin raporunu görebilir
            filter.ProjectCustomerId = customerId.Value;

            var result = await _reportService.ExportQuestionGroupAverageReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting question group average report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Rapor oluşturulurken hata oluştu." });
        }
    }

    /// <summary>
    /// Müşteri Değerlendirme Raporu - Excel export (CustomerPortal)
    /// </summary>
    [HttpPost("reports/export/customer-evaluation")]
    public async Task<IActionResult> ExportCustomerEvaluationReport([FromBody] ReportFilterDto filter)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Müşteri sadece kendi projesinin raporunu görebilir
            filter.ProjectCustomerId = customerId.Value;

            var result = await _reportService.ExportCustomerEvaluationReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting customer evaluation report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Rapor oluşturulurken hata oluştu." });
        }
    }

    /// <summary>
    /// Cezalı KL Raporu (CustomerPortal) - EvaluatorName hariç
    /// </summary>
    [HttpGet("reports/penalties")]
    public async Task<IActionResult> GetPenaltiesReport(
        [FromQuery] int? projectId,
        [FromQuery] int? organizationId,
        [FromQuery] int? checklistId,
        [FromQuery] string? penaltyType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var filter = new PenaltyFilterDto
            {
                ProjectId = projectId,
                CustomerId = customerId.Value, // Otomatik müşteri filtresi
                OrganizationId = organizationId,
                ChecklistId = checklistId,
                EvaluatorId = null, // Müşteri değerlendirici filtreleyemez
                PenaltyType = penaltyType,
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                PageSize = pageSize
            };

            var result = await _reportService.GetPenaltiesReportAsync(filter);

            // EvaluatorName alanlarını temizle (müşteri görmemeli)
            foreach (var penalty in result.Penalties)
            {
                penalty.EvaluatorName = null;
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading penalties report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Cezalı KL raporu yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Cezalı KL Raporu Excel Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/penalties/export")]
    public async Task<IActionResult> ExportPenaltiesToExcel(
        [FromQuery] int? projectId,
        [FromQuery] int? organizationId,
        [FromQuery] int? checklistId,
        [FromQuery] string? penaltyType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var filter = new PenaltyFilterDto
            {
                ProjectId = projectId,
                CustomerId = customerId.Value,
                OrganizationId = organizationId,
                ChecklistId = checklistId,
                EvaluatorId = null,
                PenaltyType = penaltyType,
                StartDate = startDate,
                EndDate = endDate,
                Page = 1,
                PageSize = int.MaxValue
            };

            var result = await _reportService.ExportPenaltiesToExcelAsync(filter, excludeEvaluator: true);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting penalties report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Cezalı KL raporu export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Öneriler Raporu (CustomerPortal) - EvaluatorName hariç
    /// </summary>
    [HttpGet("reports/suggestions")]
    public async Task<IActionResult> GetSuggestionsReport(
        [FromQuery] int? projectId,
        [FromQuery] int? checklistId,
        [FromQuery] string? searchText,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var filter = new SuggestionsFilterDto
            {
                ProjectId = projectId,
                CustomerId = customerId.Value, // Otomatik müşteri filtresi
                ChecklistId = checklistId,
                EvaluatorId = null, // Müşteri değerlendirici filtreleyemez
                SearchText = searchText,
                StartDate = startDate,
                EndDate = endDate,
                Page = page,
                PageSize = pageSize
            };

            var result = await _reportService.GetSuggestionsReportAsync(filter);

            // EvaluatorName alanlarını temizle (müşteri görmemeli)
            foreach (var suggestion in result.Suggestions)
            {
                suggestion.EvaluatorName = null;
            }

            // Değerlendirici sayısını gizle
            result.Summary.UniqueEvaluators = 0;

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading suggestions report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Öneriler raporu yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// En çok öneri yazılan sorular (CustomerPortal)
    /// </summary>
    [HttpGet("reports/suggestions/top-questions")]
    public async Task<IActionResult> GetTopSuggestedQuestions(
        [FromQuery] int? projectId,
        [FromQuery] int? checklistId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int top = 10)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var filter = new SuggestionsFilterDto
            {
                ProjectId = projectId,
                CustomerId = customerId.Value,
                ChecklistId = checklistId,
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await _reportService.GetTopSuggestedQuestionsAsync(filter, top);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading top suggested questions for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "En çok öneri yazılan sorular yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Öneriler Raporu Excel Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/suggestions/export")]
    public async Task<IActionResult> ExportSuggestionsToExcel(
        [FromQuery] int? projectId,
        [FromQuery] int? checklistId,
        [FromQuery] string? searchText,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var filter = new SuggestionsFilterDto
            {
                ProjectId = projectId,
                CustomerId = customerId.Value,
                ChecklistId = checklistId,
                EvaluatorId = null,
                SearchText = searchText,
                StartDate = startDate,
                EndDate = endDate,
                Page = 1,
                PageSize = int.MaxValue
            };

            var result = await _reportService.ExportSuggestionsToExcelAsync(filter, excludeEvaluator: true);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting suggestions report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Öneriler raporu export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Temsilci Karnesi - Personel Listesi (CustomerPortal)
    /// </summary>
    [HttpGet("reports/personnel-list")]
    public async Task<IActionResult> GetPersonnelList([FromQuery] int? organizationId)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var personnel = await _reportService.GetEvaluatedPersonnelListAsync(customerId.Value, organizationId);
            return Ok(personnel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading personnel list for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Personel listesi yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Temsilci Karnesi Raporu (CustomerPortal) - EvaluatorName hariç
    /// </summary>
    [HttpGet("reports/personnel-report-card/{personnelId}")]
    public async Task<IActionResult> GetPersonnelReportCard(
        int personnelId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                CustomerId = customerId.Value, // Otomatik müşteri filtresi
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await _reportService.GetPersonnelReportCardAsync(filter);

            if (result == null)
                return NotFound(new { message = "Temsilci bulunamadı." });

            // EvaluatorName alanlarını temizle (müşteri görmemeli)
            foreach (var evaluation in result.RecentEvaluations)
            {
                evaluation.EvaluatorName = null;
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading personnel report card for customer {CustomerId}, personnel {PersonnelId}", customerId, personnelId);
            return StatusCode(500, new { message = "Temsilci karnesi yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Temsilci Karnesi Excel Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/personnel-report-card/{personnelId}/export")]
    public async Task<IActionResult> ExportPersonnelReportCard(
        int personnelId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                CustomerId = customerId.Value,
                StartDate = startDate,
                EndDate = endDate
            };

            var result = await _reportService.ExportPersonnelReportCardToPdfAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting personnel report card for customer {CustomerId}, personnel {PersonnelId}", customerId, personnelId);
            return StatusCode(500, new { message = "Temsilci karnesi export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Proje Performans Raporu - Excel export (CustomerPortal)
    /// </summary>
    [HttpPost("reports/export/project-performance")]
    public async Task<IActionResult> ExportProjectPerformanceReport([FromBody] ReportFilterDto filter)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Müşteri sadece kendi projesinin raporunu görebilir
            filter.ProjectCustomerId = customerId.Value;

            var result = await _reportService.ExportProjectPerformanceReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting project performance report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Rapor oluşturulurken hata oluştu." });
        }
    }
}
