using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Dashboard;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

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
    private readonly IEvaluationService _evaluationService;
    private readonly ICustomerScoreThresholdService _customerScoreThresholdService;

    public CustomerPortalApiController(
        ApplicationDbContext context,
        ILogger<CustomerPortalApiController> logger,
        ILocalizationService localizationService,
        IReportService reportService,
        IEvaluationService evaluationService,
        ICustomerScoreThresholdService customerScoreThresholdService)
    {
        _context = context;
        _logger = logger;
        _localizationService = localizationService;
        _reportService = reportService;
        _evaluationService = evaluationService;
        _customerScoreThresholdService = customerScoreThresholdService;
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

        // Admin için session'dan al
        if (IsAdmin())
        {
            var sessionCustomerId = HttpContext.Session.GetInt32("AdminViewAsCustomerId");
            if (sessionCustomerId.HasValue)
                return sessionCustomerId.Value;
        }

        // Yoksa token'dan manuel parse et
        return GetCustomerIdFromToken();
    }

    /// <summary>
    /// Kullanıcı Admin mi kontrol eder
    /// </summary>
    private bool IsAdmin()
    {
        if (User.IsInRole("Admin"))
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
                return jwtToken.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
            }
            catch { }
        }
        return false;
    }

    // ==================== ADMIN MÜŞTERİ SEÇİM ====================

    /// <summary>
    /// Admin için müşteri listesi (arama destekli)
    /// </summary>
    [HttpGet("admin/customers")]
    public async Task<IActionResult> GetCustomersForAdmin([FromQuery] string? search, [FromQuery] bool includeInactive = true)
    {
        if (!IsAdmin())
            return Forbid();

        var query = _context.Customers.Where(c => !c.IsDeleted);

        if (!includeInactive)
            query = query.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(c =>
                c.CompanyName.ToLower().Contains(searchLower) ||
                (c.Code != null && c.Code.ToLower().Contains(searchLower)));
        }

        var customers = await query
            .OrderBy(c => c.CompanyName)
            .Select(c => new
            {
                c.Id,
                c.CompanyName,
                c.Code,
                c.IsActive
            })
            .ToListAsync();

        return Ok(customers);
    }

    /// <summary>
    /// Admin müşteri seçimi yapar (session'a yazar)
    /// </summary>
    [HttpPost("admin/view-as-customer/{customerId}")]
    public async Task<IActionResult> SetViewAsCustomer(int customerId)
    {
        if (!IsAdmin())
            return Forbid();

        var customer = await _context.Customers
            .Where(c => c.Id == customerId && !c.IsDeleted)
            .Select(c => new { c.Id, c.CompanyName, c.Code })
            .FirstOrDefaultAsync();

        if (customer == null)
            return NotFound(new { message = "Müşteri bulunamadı." });

        HttpContext.Session.SetInt32("AdminViewAsCustomerId", customerId);
        HttpContext.Session.SetString("AdminViewAsCustomerName", customer.CompanyName);

        return Ok(new {
            success = true,
            customerId = customer.Id,
            customerName = customer.CompanyName
        });
    }

    /// <summary>
    /// Admin'in şu an seçili müşterisini döndürür
    /// </summary>
    [HttpGet("admin/current-customer")]
    public IActionResult GetCurrentCustomer()
    {
        if (!IsAdmin())
            return Forbid();

        var customerId = HttpContext.Session.GetInt32("AdminViewAsCustomerId");
        var customerName = HttpContext.Session.GetString("AdminViewAsCustomerName");

        if (!customerId.HasValue)
            return Ok(new { hasSelection = false });

        return Ok(new {
            hasSelection = true,
            customerId = customerId.Value,
            customerName = customerName
        });
    }

    /// <summary>
    /// Admin müşteri seçimini temizler
    /// </summary>
    [HttpDelete("admin/view-as-customer")]
    public IActionResult ClearViewAsCustomer()
    {
        if (!IsAdmin())
            return Forbid();

        HttpContext.Session.Remove("AdminViewAsCustomerId");
        HttpContext.Session.Remove("AdminViewAsCustomerName");

        return Ok(new { success = true });
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
        // CustomerPersonnel token'ında ClaimTypes.NameIdentifier kullanılıyor
        // Önce UserType claim'ini kontrol et - CustomerPersonnel mi?
        var userType = User.FindFirst("UserType")?.Value;
        if (userType == "CustomerPersonnel")
        {
            var nameIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(nameIdClaim, out var personnelId))
                return personnelId;
        }

        // Eski yöntem - PersonnelId claim'i (geriye uyumluluk)
        var personnelIdClaim = User.FindFirst("PersonnelId")?.Value;
        if (int.TryParse(personnelIdClaim, out var pId))
            return pId;

        // Token'dan manuel parse et
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            try
            {
                var token = authHeader.Substring("Bearer ".Length);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // UserType kontrolü
                var userTypeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "UserType")?.Value;
                if (userTypeClaim == "CustomerPersonnel")
                {
                    var nameIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid")?.Value;
                    if (int.TryParse(nameIdClaim, out var personnelId))
                        return personnelId;
                }

                // Eski yöntem
                var claim = jwtToken.Claims.FirstOrDefault(c => c.Type == "PersonnelId")?.Value;
                if (int.TryParse(claim, out pId))
                    return pId;
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
    /// Rol bazlı personel erişim kontrolü - Organizasyon bazında hibrit mantık
    /// </summary>
    /// <returns>
    /// null = Tüm personeli görebilir (Admin/Manager)
    /// List = Sadece bu ID'lerdeki personeli görebilir
    /// </returns>
    private async Task<List<int>?> GetAllowedPersonnelIdsAsync()
    {
        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();

        // Admin ve CustomerManager tüm personeli görebilir
        if (role == "Admin" || role == "CustomerManager")
            return null;

        // CustomerSupervisor - Organizasyon bazında hibrit kontrol
        if (role == "CustomerSupervisor" && personnelId.HasValue)
        {
            // 1. Süpervizörün atandığı organizasyonları bul
            var myOrgIds = await _context.CustomerPersonnelOrganizations
                .Where(cpo => cpo.CustomerPersonnelId == personnelId.Value && !cpo.IsDeleted)
                .Select(cpo => cpo.CustomerOrganizationId)
                .Distinct()
                .ToListAsync();

            // Hiçbir organizasyona atanmamış → TÜM veriyi görebilir (null = filtre yok)
            if (!myOrgIds.Any())
                return null;

            // 2. Bu organizasyonlarda süpervizör olduğu personeller
            var supervisedPersonnel = await _context.CustomerPersonnelOrganizations
                .Where(cpo => myOrgIds.Contains(cpo.CustomerOrganizationId) &&
                             cpo.SupervisorId == personnelId.Value &&
                             !cpo.IsDeleted)
                .Select(cpo => new { cpo.CustomerOrganizationId, cpo.CustomerPersonnelId })
                .ToListAsync();

            // 3. Hangi organizasyonlarda altında personel var?
            var orgsWithTeam = supervisedPersonnel
                .Select(x => x.CustomerOrganizationId)
                .Distinct()
                .ToHashSet();

            // 4. Altında personel olmayan organizasyonlar
            var orgsWithoutTeam = myOrgIds.Except(orgsWithTeam).ToList();

            var result = new HashSet<int>();

            // Altında personel olan org'lardan sadece o personeller
            foreach (var p in supervisedPersonnel)
                result.Add(p.CustomerPersonnelId);

            // Altında personel olmayan org'lardan TÜM personeller
            if (orgsWithoutTeam.Any())
            {
                var allPersonnelInEmptyOrgs = await _context.CustomerPersonnelOrganizations
                    .Where(cpo => orgsWithoutTeam.Contains(cpo.CustomerOrganizationId) && !cpo.IsDeleted)
                    .Select(cpo => cpo.CustomerPersonnelId)
                    .Distinct()
                    .ToListAsync();

                foreach (var id in allPersonnelInEmptyOrgs)
                    result.Add(id);
            }

            // Kendisini de ekle
            result.Add(personnelId.Value);

            return result.ToList();
        }

        // CustomerOperator sadece kendini görebilir
        if (personnelId.HasValue)
            return new List<int> { personnelId.Value };

        return new List<int>(); // Hiçbir şey göremez
    }

    /// <summary>
    /// Rol bazlı organizasyon erişim kontrolü
    /// </summary>
    /// <returns>
    /// null = Tüm organizasyonları görebilir (Admin/Manager veya org'a atanmamış Supervisor)
    /// List = Sadece bu ID'lerdeki organizasyonları görebilir
    /// </returns>
    private async Task<List<int>?> GetAllowedOrganizationIdsAsync()
    {
        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();

        // Admin ve CustomerManager tüm organizasyonları görebilir
        if (role == "Admin" || role == "CustomerManager")
            return null;

        // CustomerSupervisor - Organizasyon bazında kontrol
        if (role == "CustomerSupervisor" && personnelId.HasValue)
        {
            // Süpervizörün atandığı organizasyonları bul
            var myOrgIds = await _context.CustomerPersonnelOrganizations
                .Where(cpo => cpo.CustomerPersonnelId == personnelId.Value && !cpo.IsDeleted)
                .Select(cpo => cpo.CustomerOrganizationId)
                .Distinct()
                .ToListAsync();

            // Hiçbir organizasyona atanmamış → TÜM organizasyonları görebilir
            if (!myOrgIds.Any())
                return null;

            // Atandığı organizasyonları döndür
            return myOrgIds;
        }

        // CustomerOperator - Kendi organizasyonlarını görebilir
        if (role == "CustomerOperator" && personnelId.HasValue)
        {
            var myOrgIds = await _context.CustomerPersonnelOrganizations
                .Where(cpo => cpo.CustomerPersonnelId == personnelId.Value && !cpo.IsDeleted)
                .Select(cpo => cpo.CustomerOrganizationId)
                .Distinct()
                .ToListAsync();

            return myOrgIds.Any() ? myOrgIds : new List<int>();
        }

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
    public async Task<IActionResult> GetMonthlyTrend([FromQuery] int? projectId = null)
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

        // Proje filtresi
        if (projectId.HasValue)
        {
            evaluationsQuery = evaluationsQuery.Where(e => e.Assignment!.ProjectId == projectId.Value);
        }

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

            // Cezalı KL sayıları
            var yellowCardCount = monthEvals.Sum(e => e.YellowCardCount);
            var redCardCount = monthEvals.Sum(e => e.RedCardCount);

            monthlyData.Add(new
            {
                month = monthStart.ToString("MMM", new System.Globalization.CultureInfo("tr-TR")),
                year = monthStart.Year,
                count = monthEvals.Count,
                averageScore = Math.Round(avgScore, 1),
                yellowCardCount,
                redCardCount
            });
        }

        return Ok(monthlyData);
    }

    /// <summary>
    /// Soru grupları bazlı aylık trend (son 12 ay)
    /// </summary>
    [HttpGet("dashboard/question-group-trend")]
    public async Task<IActionResult> GetQuestionGroupTrend([FromQuery] List<int>? projectIds = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);

        // Projeleri getir (dropdown için)
        var projects = await _context.Projects
            .Where(p => p.CustomerId == customerId && p.IsActive && !p.IsDeleted)
            .Select(p => new { p.Id, p.Name })
            .OrderBy(p => p.Name)
            .ToListAsync();

        // Cevapları getir
        var answersQuery = _context.Answers
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Assignment)
                    .ThenInclude(a => a!.Project)
            .Include(a => a.Question)
            .Where(a => a.Evaluation.Assignment != null &&
                        a.Evaluation.Assignment.Project != null &&
                        a.Evaluation.Assignment.Project.CustomerId == customerId &&
                        a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed &&
                        a.Evaluation.CreatedAt >= startDate &&
                        a.Question.GroupName != null &&
                        a.Question.GroupName != "" &&
                        a.EarnedPoints.HasValue &&
                        a.Question.WeightPoints > 0);

        if (projectIds?.Any() == true)
        {
            answersQuery = answersQuery.Where(a => projectIds.Contains(a.Evaluation.Assignment!.ProjectId));
        }

        if (allowedPersonnelIds != null)
        {
            answersQuery = answersQuery.Where(a =>
                a.Evaluation.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(a.Evaluation.EvaluatedCustomerPersonnelId.Value));
        }

        var answers = await answersQuery
            .Select(a => new
            {
                a.Evaluation.CreatedAt,
                a.Question.GroupName,
                a.EarnedPoints,
                a.Question.WeightPoints
            })
            .ToListAsync();

        // Grup adlarını al
        var groupNames = answers.Select(a => a.GroupName).Distinct().OrderBy(g => g).ToList();

        // Ay etiketlerini oluştur
        var monthLabels = new List<string>();
        for (int i = 0; i < 12; i++)
        {
            var monthDate = startDate.AddMonths(i);
            monthLabels.Add(monthDate.ToString("MMM", new System.Globalization.CultureInfo("tr-TR")));
        }

        // Her grup için aylık trend
        var groupTrends = new List<object>();
        foreach (var groupName in groupNames)
        {
            var monthlyScores = new List<double>();
            for (int i = 0; i < 12; i++)
            {
                var monthStart = startDate.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1);

                var monthAnswers = answers.Where(a =>
                    a.GroupName == groupName &&
                    a.CreatedAt >= monthStart &&
                    a.CreatedAt < monthEnd).ToList();

                double avgScore = 0;
                if (monthAnswers.Any())
                {
                    // Her cevabın yüzdesini hesapla ve ortalamasını al
                    avgScore = monthAnswers.Average(a =>
                        (double)(a.EarnedPoints!.Value / a.WeightPoints * 100));
                }
                monthlyScores.Add(Math.Round(avgScore, 1));
            }

            groupTrends.Add(new
            {
                groupName,
                scores = monthlyScores
            });
        }

        return Ok(new
        {
            projects,
            selectedProjectIds = projectIds,
            monthLabels,
            groupTrends
        });
    }

    /// <summary>
    /// Sorular bazlı aylık trend (son 12 ay)
    /// </summary>
    [HttpGet("dashboard/question-trend")]
    public async Task<IActionResult> GetQuestionTrend(
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] string? groupName = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);

        // Cevapları getir
        var answersQuery = _context.Answers
            .Include(a => a.Evaluation)
                .ThenInclude(e => e.Assignment)
                    .ThenInclude(a => a!.Project)
            .Include(a => a.Question)
            .Where(a => a.Evaluation.Assignment != null &&
                        a.Evaluation.Assignment.Project != null &&
                        a.Evaluation.Assignment.Project.CustomerId == customerId &&
                        a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed &&
                        a.Evaluation.CreatedAt >= startDate &&
                        a.EarnedPoints.HasValue &&
                        a.Question.WeightPoints > 0 &&
                        a.Question.ScoringTypeId == ScoringTypes.Ids.Scored); // Sadece puanlı sorular

        if (projectIds?.Any() == true)
        {
            answersQuery = answersQuery.Where(a => projectIds.Contains(a.Evaluation.Assignment!.ProjectId));
        }

        if (!string.IsNullOrEmpty(groupName))
        {
            answersQuery = answersQuery.Where(a => a.Question.GroupName == groupName);
        }

        if (allowedPersonnelIds != null)
        {
            answersQuery = answersQuery.Where(a =>
                a.Evaluation.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(a.Evaluation.EvaluatedCustomerPersonnelId.Value));
        }

        var answers = await answersQuery
            .Select(a => new
            {
                a.Evaluation.CreatedAt,
                a.QuestionId,
                QuestionText = a.Question.Text,
                a.Question.GroupName,
                a.Question.Order,
                a.EarnedPoints,
                a.Question.WeightPoints
            })
            .ToListAsync();

        // Soruları al (en çok cevap alan ilk 10 soru)
        var questions = answers
            .GroupBy(a => new { a.QuestionId, a.QuestionText, a.GroupName, a.Order })
            .Select(g => new
            {
                g.Key.QuestionId,
                g.Key.QuestionText,
                g.Key.GroupName,
                g.Key.Order,
                AnswerCount = g.Count()
            })
            .OrderByDescending(q => q.AnswerCount)
            .Take(10)
            .OrderBy(q => q.GroupName)
            .ThenBy(q => q.Order)
            .ToList();

        // Ay etiketlerini oluştur
        var monthLabels = new List<string>();
        for (int i = 0; i < 12; i++)
        {
            var monthDate = startDate.AddMonths(i);
            monthLabels.Add(monthDate.ToString("MMM", new System.Globalization.CultureInfo("tr-TR")));
        }

        // Her soru için aylık trend
        var questionTrends = new List<object>();
        foreach (var question in questions)
        {
            var monthlyScores = new List<double>();
            for (int i = 0; i < 12; i++)
            {
                var monthStart = startDate.AddMonths(i);
                var monthEnd = monthStart.AddMonths(1);

                var monthAnswers = answers.Where(a =>
                    a.QuestionId == question.QuestionId &&
                    a.CreatedAt >= monthStart &&
                    a.CreatedAt < monthEnd).ToList();

                double avgScore = 0;
                if (monthAnswers.Any())
                {
                    avgScore = monthAnswers.Average(a =>
                        (double)(a.EarnedPoints!.Value / a.WeightPoints * 100));
                }
                monthlyScores.Add(Math.Round(avgScore, 1));
            }

            questionTrends.Add(new
            {
                questionId = question.QuestionId,
                questionText = question.QuestionText.Length > 50
                    ? question.QuestionText.Substring(0, 47) + "..."
                    : question.QuestionText,
                groupName = question.GroupName,
                scores = monthlyScores
            });
        }

        // Grup adlarını getir (filtre için)
        var groupNames = await _context.Questions
            .Where(q => q.GroupName != null && q.GroupName != "")
            .Select(q => q.GroupName)
            .Distinct()
            .OrderBy(g => g)
            .ToListAsync();

        return Ok(new
        {
            groupNames,
            selectedGroupName = groupName,
            monthLabels,
            questionTrends
        });
    }

    /// <summary>
    /// Puan dağılımı (tarih filtreli)
    /// </summary>
    [HttpGet("dashboard/score-distribution")]
    public async Task<IActionResult> GetScoreDistribution([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
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
                        e.StatusId == EvaluationStatuses.Ids.Completed);

        // Tarih filtresi
        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => e.CreatedAt >= start);
        }
        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => e.CreatedAt <= end);
        }

        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        // Proje tipi ile birlikte puanları çek
        var scores = await evaluationsQuery
            .Select(e => new
            {
                ProjectTypeId = e.Assignment!.Project!.ProjectTypeId,
                Score = e.ScorePercentage ?? 0
            })
            .ToListAsync();

        // Değerlemesi olmayan proje tipleri gelmesin
        var groupedByType = scores.GroupBy(s => s.ProjectTypeId).ToList();
        if (groupedByType.Count == 0)
            return Ok(new List<object>());

        // Müşteriye özel eşikleri al (yoksa global fallback)
        var thresholds = await _customerScoreThresholdService.GetAllAsync(customerId.Value);

        var result = groupedByType.Select(g =>
        {
            var threshold = thresholds.FirstOrDefault(t => t.ProjectTypeId == g.Key);
            var successThreshold = threshold?.SuccessThreshold ?? 80m;
            var warningThreshold = threshold?.WarningThreshold ?? 60m;
            var projectType = ProjectTypes.GetById(g.Key);

            var typeScores = g.Select(s => s.Score).ToList();

            return new
            {
                projectTypeId = g.Key,
                projectTypeName = threshold?.ProjectTypeName ?? projectType?.Description ?? "Bilinmeyen",
                projectTypeIcon = threshold?.ProjectTypeIcon ?? projectType?.Icon ?? "bi-folder",
                projectTypeColor = threshold?.ProjectTypeColor ?? projectType?.CssClass ?? "bg-secondary",
                successThreshold,
                warningThreshold,
                success = typeScores.Count(s => s >= successThreshold),
                warning = typeScores.Count(s => s >= warningThreshold && s < successThreshold),
                danger = typeScores.Count(s => s < warningThreshold),
                total = typeScores.Count
            };
        })
        .OrderBy(r => r.projectTypeId)
        .ToList();

        return Ok(result);
    }

    /// <summary>
    /// Puan dağılımı kategorisindeki değerlendirmeler (tıklanan renge göre)
    /// </summary>
    [HttpGet("dashboard/score-distribution/evaluations")]
    public async Task<IActionResult> GetScoreDistributionEvaluations(
        [FromQuery] string category,
        [FromQuery] int projectTypeId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a!.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Where(e => e.Assignment != null && e.Assignment.Project != null &&
                        e.Assignment.Project.CustomerId == customerId &&
                        e.Assignment.Project.ProjectTypeId == projectTypeId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed);

        // Tarih filtresi
        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => e.CreatedAt >= start);
        }
        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => e.CreatedAt <= end);
        }

        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        // Eşikleri al (müşteriye özel, yoksa global fallback)
        var thresholds = await _customerScoreThresholdService.GetAllAsync(customerId.Value);
        var threshold = thresholds.FirstOrDefault(t => t.ProjectTypeId == projectTypeId);
        var st = threshold?.SuccessThreshold ?? 80m;
        var wt = threshold?.WarningThreshold ?? 60m;

        // Kategori filtresi (eşiklere göre)
        switch (category?.ToLower())
        {
            case "success":
                evaluationsQuery = evaluationsQuery.Where(e => (e.ScorePercentage ?? 0) >= st);
                break;
            case "warning":
                evaluationsQuery = evaluationsQuery.Where(e => (e.ScorePercentage ?? 0) >= wt && (e.ScorePercentage ?? 0) < st);
                break;
            case "danger":
                evaluationsQuery = evaluationsQuery.Where(e => e.ScorePercentage == null || (e.ScorePercentage ?? 0) < wt);
                break;
            default:
                return BadRequest(new { message = "Geçersiz kategori. Geçerli değerler: success, warning, danger" });
        }

        var total = await evaluationsQuery.CountAsync();

        var evaluations = await evaluationsQuery
            .OrderByDescending(e => e.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new
            {
                e.Id,
                evaluationDate = e.CallDate ?? e.CompletedAt ?? e.CreatedAt,
                projectName = e.Assignment!.Project!.Name,
                personnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : "-",
                organizationName = e.EvaluatedOrganization != null ? e.EvaluatedOrganization.Name : "-",
                score = e.ScorePercentage ?? 0,
                e.YellowCardCount,
                e.RedCardCount
            })
            .ToListAsync();

        return Ok(new { items = evaluations, total, page, pageSize });
    }

    /// <summary>
    /// Puan dağılımı kategorisindeki değerlendirmeleri Excel'e export et
    /// </summary>
    [HttpGet("dashboard/score-distribution/export")]
    public async Task<IActionResult> ExportScoreDistributionEvaluations(
        [FromQuery] string category,
        [FromQuery] int projectTypeId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a!.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Where(e => e.Assignment != null && e.Assignment.Project != null &&
                        e.Assignment.Project.CustomerId == customerId &&
                        e.Assignment.Project.ProjectTypeId == projectTypeId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed);

        // Tarih filtresi
        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => e.CreatedAt >= start);
        }
        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            evaluationsQuery = evaluationsQuery.Where(e => e.CreatedAt <= end);
        }

        if (allowedPersonnelIds != null)
        {
            evaluationsQuery = evaluationsQuery.Where(e =>
                e.EvaluatedCustomerPersonnelId.HasValue &&
                allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
        }

        // Eşikleri al
        var thresholds = await _customerScoreThresholdService.GetAllAsync(customerId.Value);
        var threshold = thresholds.FirstOrDefault(t => t.ProjectTypeId == projectTypeId);
        var st = threshold?.SuccessThreshold ?? 80m;
        var wt = threshold?.WarningThreshold ?? 60m;

        var categoryLabel = "";
        switch (category?.ToLower())
        {
            case "success":
                evaluationsQuery = evaluationsQuery.Where(e => (e.ScorePercentage ?? 0) >= st);
                categoryLabel = $"Başarılı ({st}+)";
                break;
            case "warning":
                evaluationsQuery = evaluationsQuery.Where(e => (e.ScorePercentage ?? 0) >= wt && (e.ScorePercentage ?? 0) < st);
                categoryLabel = $"Uyarı ({wt}-{st})";
                break;
            case "danger":
                evaluationsQuery = evaluationsQuery.Where(e => e.ScorePercentage == null || (e.ScorePercentage ?? 0) < wt);
                categoryLabel = $"Başarısız (<{wt})";
                break;
            default:
                return BadRequest(new { message = "Geçersiz kategori" });
        }

        var evaluations = await evaluationsQuery
            .OrderByDescending(e => e.CreatedAt)
            .Select(e => new
            {
                evaluationDate = e.CallDate ?? e.CompletedAt ?? e.CreatedAt,
                projectName = e.Assignment!.Project!.Name,
                personnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : "-",
                organizationName = e.EvaluatedOrganization != null ? e.EvaluatedOrganization.Name : "-",
                score = e.ScorePercentage ?? 0,
                yellowCardCount = e.YellowCardCount,
                redCardCount = e.RedCardCount
            })
            .ToListAsync();

        // Excel oluştur
        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Değerlendirmeler");

        // Başlık
        worksheet.Cell(1, 1).Value = "Tarih";
        worksheet.Cell(1, 2).Value = "Proje";
        worksheet.Cell(1, 3).Value = "Personel";
        worksheet.Cell(1, 4).Value = "Organizasyon";
        worksheet.Cell(1, 5).Value = "Puan";
        worksheet.Cell(1, 6).Value = "Sarı Kart";
        worksheet.Cell(1, 7).Value = "Kırmızı Kart";

        // Başlık stili
        var headerRange = worksheet.Range(1, 1, 1, 7);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

        // Veri
        for (int i = 0; i < evaluations.Count; i++)
        {
            var row = i + 2;
            var eval = evaluations[i];
            worksheet.Cell(row, 1).Value = eval.evaluationDate.ToString("dd.MM.yyyy");
            worksheet.Cell(row, 2).Value = eval.projectName;
            worksheet.Cell(row, 3).Value = eval.personnelName;
            worksheet.Cell(row, 4).Value = eval.organizationName;
            worksheet.Cell(row, 5).Value = eval.score;
            worksheet.Cell(row, 6).Value = eval.yellowCardCount;
            worksheet.Cell(row, 7).Value = eval.redCardCount;
        }

        // Kolon genişliklerini ayarla
        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"PuanDagilimi_{categoryLabel.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    /// <summary>
    /// Dashboard chart'larini Excel'e export et (chart gorseli + veri tablosu)
    /// </summary>
    [HttpPost("dashboard/charts/export")]
    public IActionResult ExportChartToExcel([FromBody] ChartExportRequestDto dto)
    {
        if (string.IsNullOrEmpty(dto.ChartImage) || string.IsNullOrEmpty(dto.DataJson))
            return BadRequest(new { message = "Chart image ve data gereklidir." });

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Chart");

        var title = dto.ChartTitle ?? dto.ChartType;
        // Row 1: Title (bold, merged)
        worksheet.Cell(1, 1).Value = title;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 6).Merge();

        // Row 2: Date
        worksheet.Cell(2, 1).Value = DateTime.Now.ToString("dd.MM.yyyy HH:mm");
        worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        // Row 4+: Chart image
        var imageStartRow = 4;
        var imageRowCount = 15; // approximate rows the image will span
        try
        {
            var base64Data = dto.ChartImage;
            if (base64Data.Contains(","))
                base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);

            var imageBytes = Convert.FromBase64String(base64Data);
            using var imgStream = new MemoryStream(imageBytes);
            var picture = worksheet.AddPicture(imgStream, XLPictureFormat.Png);
            picture.MoveTo(worksheet.Cell(imageStartRow, 1));
            picture.Scale(0.6);

            // Estimate how many rows the image takes
            imageRowCount = (int)Math.Ceiling(picture.Height / 15.0 * 0.6);
            if (imageRowCount < 15) imageRowCount = 15;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chart image embed failed");
            worksheet.Cell(imageStartRow, 1).Value = "(Grafik goruntusu yuklenemedi)";
        }

        // Data table starts after image
        var dataStartRow = imageStartRow + imageRowCount + 2;

        try
        {
            switch (dto.ChartType?.ToLower())
            {
                case "monthly-trend":
                    BuildMonthlyTrendTable(worksheet, dto.DataJson, dataStartRow);
                    break;
                case "score-distribution":
                    BuildScoreDistributionTable(worksheet, dto.DataJson, dataStartRow);
                    break;
                case "question-trend":
                    BuildQuestionTrendTable(worksheet, dto.DataJson, dataStartRow);
                    break;
                default:
                    worksheet.Cell(dataStartRow, 1).Value = "Bilinmeyen chart tipi: " + dto.ChartType;
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chart data table build failed for type {ChartType}", dto.ChartType);
            worksheet.Cell(dataStartRow, 1).Value = "(Veri tablosu olusturulamadi)";
        }

        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"{title.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private void BuildMonthlyTrendTable(IXLWorksheet ws, string dataJson, int startRow)
    {
        var items = JsonSerializer.Deserialize<List<JsonElement>>(dataJson);
        if (items == null || items.Count == 0) return;

        // Header
        ws.Cell(startRow, 1).Value = "Ay";
        ws.Cell(startRow, 2).Value = "Ort. Puan";
        ws.Cell(startRow, 3).Value = "Degerlendirme";
        ws.Cell(startRow, 4).Value = "Sari Kart";
        ws.Cell(startRow, 5).Value = "Kirmizi Kart";

        var headerRange = ws.Range(startRow, 1, startRow, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        for (int i = 0; i < items.Count; i++)
        {
            var row = startRow + 1 + i;
            var item = items[i];
            ws.Cell(row, 1).Value = item.GetProperty("month").GetString() ?? "";
            ws.Cell(row, 2).Value = item.GetProperty("averageScore").GetDouble();
            ws.Cell(row, 3).Value = item.GetProperty("count").GetInt32();
            ws.Cell(row, 4).Value = GetJsonInt(item, "yellowCardCount");
            ws.Cell(row, 5).Value = GetJsonInt(item, "redCardCount");
        }
    }

    private void BuildScoreDistributionTable(IXLWorksheet ws, string dataJson, int startRow)
    {
        var data = JsonSerializer.Deserialize<JsonElement>(dataJson);

        // Header
        ws.Cell(startRow, 1).Value = "Kategori";
        ws.Cell(startRow, 2).Value = "Adet";

        var headerRange = ws.Range(startRow, 1, startRow, 2);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        var categories = new[] {
            ("Mukemmel (90+)", "excellent"),
            ("Iyi (80-89)", "good"),
            ("Orta (60-79)", "average"),
            ("Dusuk (<60)", "poor")
        };

        for (int i = 0; i < categories.Length; i++)
        {
            var row = startRow + 1 + i;
            ws.Cell(row, 1).Value = categories[i].Item1;
            ws.Cell(row, 2).Value = GetJsonInt(data, categories[i].Item2);
        }
    }

    private void BuildQuestionTrendTable(IXLWorksheet ws, string dataJson, int startRow)
    {
        var data = JsonSerializer.Deserialize<JsonElement>(dataJson);
        if (data.ValueKind != JsonValueKind.Object) return;

        var monthLabels = new List<string>();
        if (data.TryGetProperty("monthLabels", out var labelsEl) && labelsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var label in labelsEl.EnumerateArray())
                monthLabels.Add(label.GetString() ?? "");
        }

        // Determine which property holds trends
        JsonElement trendsEl;
        if (data.TryGetProperty("groupTrends", out trendsEl) || data.TryGetProperty("questionTrends", out trendsEl))
        {
            // ok
        }
        else
        {
            return;
        }

        if (trendsEl.ValueKind != JsonValueKind.Array) return;
        var trends = trendsEl.EnumerateArray().ToList();
        if (trends.Count == 0) return;

        // Header row: "Soru/Grup", Month1, Month2, ...
        ws.Cell(startRow, 1).Value = "Soru / Grup";
        for (int j = 0; j < monthLabels.Count; j++)
        {
            ws.Cell(startRow, 2 + j).Value = monthLabels[j];
        }

        var colCount = 1 + monthLabels.Count;
        var headerRange = ws.Range(startRow, 1, startRow, colCount);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data rows
        for (int i = 0; i < trends.Count; i++)
        {
            var row = startRow + 1 + i;
            var trend = trends[i];

            // Name from groupName or questionText
            var name = "";
            if (trend.TryGetProperty("groupName", out var gn)) name = gn.GetString() ?? "";
            else if (trend.TryGetProperty("questionText", out var qt)) name = qt.GetString() ?? "";

            ws.Cell(row, 1).Value = name;

            if (trend.TryGetProperty("scores", out var scoresEl) && scoresEl.ValueKind == JsonValueKind.Array)
            {
                var scores = scoresEl.EnumerateArray().ToList();
                for (int j = 0; j < scores.Count && j < monthLabels.Count; j++)
                {
                    if (scores[j].ValueKind == JsonValueKind.Number)
                        ws.Cell(row, 2 + j).Value = scores[j].GetDouble();
                    else if (scores[j].ValueKind == JsonValueKind.Null)
                        ws.Cell(row, 2 + j).Value = "";
                }
            }
        }
    }

    private static int GetJsonInt(JsonElement el, string property)
    {
        if (el.TryGetProperty(property, out var val) && val.ValueKind == JsonValueKind.Number)
            return val.GetInt32();
        return 0;
    }

    /// <summary>
    /// Müşterinin projeleri
    /// </summary>
    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects([FromQuery] int? projectTypeId = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        // Supervisor filtrelemesi
        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var query = _context.Projects
            .Where(p => p.CustomerId == customerId && p.IsActive && !p.IsDeleted);

        // Proje tipi filtresi
        if (projectTypeId.HasValue)
            query = query.Where(p => p.ProjectTypeId == projectTypeId.Value);

        // Supervisor için sadece kendi personelinin değerlendirildiği projeler
        if (allowedPersonnelIds != null)
        {
            var projectIdsWithPersonnel = await _context.Evaluations
                .Where(e => e.EvaluatedCustomerPersonnelId.HasValue &&
                           allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                           e.Assignment.Project.CustomerId == customerId &&
                           e.StatusId == EvaluationStatuses.Ids.Completed)
                .Select(e => e.Assignment.ProjectId)
                .Distinct()
                .ToListAsync();

            query = query.Where(p => projectIdsWithPersonnel.Contains(p.Id));
        }

        var projects = await query
            .Select(p => new
            {
                Id = p.Id,
                Name = p.Name,
                Code = p.Code,
                City = "",
                Address = "",
                IsActive = p.IsActive,
                evaluationCount = allowedPersonnelIds == null
                    ? _context.Evaluations.Count(e => e.Assignment.ProjectId == p.Id &&
                        e.StatusId == EvaluationStatuses.Ids.Completed)
                    : _context.Evaluations.Count(e => e.Assignment.ProjectId == p.Id &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.EvaluatedCustomerPersonnelId.HasValue &&
                        allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value)),
                averageScore = allowedPersonnelIds == null
                    ? _context.Evaluations
                        .Where(e => e.Assignment.ProjectId == p.Id && e.ScorePercentage.HasValue &&
                            e.StatusId == EvaluationStatuses.Ids.Completed)
                        .Average(e => (double?)e.ScorePercentage) ?? 0
                    : _context.Evaluations
                        .Where(e => e.Assignment.ProjectId == p.Id && e.ScorePercentage.HasValue &&
                            e.StatusId == EvaluationStatuses.Ids.Completed &&
                            e.EvaluatedCustomerPersonnelId.HasValue &&
                            allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value))
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

        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Where(e => e.Assignment != null && e.Assignment.Project != null &&
                        e.Assignment.Project.CustomerId == customerId &&
                        e.StatusId != EvaluationStatuses.Ids.Cancelled); // Taslaklar dahil, iptal edilenler hariç

        // Rol bazlı filtreleme
        // Manager/Supervisor: Kendi yaptıkları değerlendirmeler (EvaluatorCustomerPersonnelId)
        // Operator: Kendilerinin değerlendirildiği kayıtlar (EvaluatedCustomerPersonnelId)
        if (role == "CustomerOperator" && personnelId.HasValue)
        {
            // Operator: Sadece kendisinin değerlendirildiği kayıtlar
            query = query.Where(e => e.EvaluatedCustomerPersonnelId == personnelId.Value);
        }
        else if (role == "CustomerSupervisor" && personnelId.HasValue)
        {
            // Supervisor: Kendi yaptığı değerlendirmeler + allowedPersonnelIds'deki personelin değerlendirildiği kayıtlar
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null)
            {
                query = query.Where(e =>
                    e.EvaluatorCustomerPersonnelId == personnelId.Value ||
                    (e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value)));
            }
        }
        // Manager ve Admin: Tüm kayıtları görür (ek filtre yok)

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
                scoringMethodId = e.Assignment.Checklist != null ? e.Assignment.Checklist.ScoringMethodId : 1,
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
            scoringMethod = ScoringMethods.GetById(e.scoringMethodId)?.SystemName ?? "Maximum",
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
    public async Task<IActionResult> GetProjectPerformance(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] bool? isInternal = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        // Supervisor filtrelemesi
        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
        var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

        var start = startDate ?? DateTime.UtcNow.AddMonths(-3);
        var end = endDate ?? DateTime.UtcNow;

        // UTC'ye çevir
        if (start.Kind == DateTimeKind.Unspecified)
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (end.Kind == DateTimeKind.Unspecified)
            end = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var projectsQuery = _context.Projects
            .Where(p => p.CustomerId == customerId && !p.IsDeleted);

        // Project filter
        if (projectIds?.Any() == true)
            projectsQuery = projectsQuery.Where(p => projectIds.Contains(p.Id));

        var projects = await projectsQuery.ToListAsync();

        var evalQuery = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId == customerId
                && e.CreatedAt >= start
                && e.CreatedAt <= end
                && e.StatusId == EvaluationStatuses.Ids.Completed);

        // İç/Dış dinleme filtresi
        if (isInternal == true)
            evalQuery = evalQuery.Where(e => e.EvaluatorCustomerPersonnelId != null);
        else if (isInternal == false)
            evalQuery = evalQuery.Where(e => e.EvaluatorId != null);

        // Supervisor personel filtresi
        if (allowedPersonnelIds != null)
            evalQuery = evalQuery.Where(e => e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));

        // Supervisor organizasyon filtresi
        if (allowedOrgIds != null)
            evalQuery = evalQuery.Where(e => e.EvaluatedOrganizationId.HasValue && allowedOrgIds.Contains(e.EvaluatedOrganizationId.Value));

        // Project filter
        if (projectIds?.Any() == true)
            evalQuery = evalQuery.Where(e => projectIds.Contains(e.Assignment.ProjectId));

        // Organization filter
        if (organizationIds?.Any() == true)
            evalQuery = evalQuery.Where(e => e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value));

        var evaluations = await evalQuery.ToListAsync();

        // Supervisor için sadece değerlendirmesi olan projeleri göster
        var projectIdsWithEvaluations = evaluations.Select(e => e.Assignment.ProjectId).Distinct().ToHashSet();
        var filteredProjects = allowedPersonnelIds != null
            ? projects.Where(p => projectIdsWithEvaluations.Contains(p.Id)).ToList()
            : projects;

        var projectPerformance = filteredProjects.Select(p =>
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
    public async Task<IActionResult> GetReportSummary(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] bool? isInternal = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        // Supervisor filtrelemesi
        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
        var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

        var start = startDate ?? DateTime.UtcNow.AddMonths(-3);
        var end = endDate ?? DateTime.UtcNow;

        if (start.Kind == DateTimeKind.Unspecified)
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (end.Kind == DateTimeKind.Unspecified)
            end = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId == customerId
                && e.CreatedAt >= start
                && e.CreatedAt <= end
                && e.StatusId == EvaluationStatuses.Ids.Completed);

        // İç/Dış dinleme filtresi
        if (isInternal == true)
            query = query.Where(e => e.EvaluatorCustomerPersonnelId != null);
        else if (isInternal == false)
            query = query.Where(e => e.EvaluatorId != null);

        // Supervisor personel filtresi
        if (allowedPersonnelIds != null)
            query = query.Where(e => e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));

        // Supervisor organizasyon filtresi
        if (allowedOrgIds != null)
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && allowedOrgIds.Contains(e.EvaluatedOrganizationId.Value));

        // Project filter
        if (projectIds?.Any() == true)
            query = query.Where(e => projectIds.Contains(e.Assignment.ProjectId));

        // Organization filter
        if (organizationIds?.Any() == true)
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value));

        var evaluations = await query.ToListAsync();

        // Supervisor için sadece değerlendirmesi olan proje sayısı
        int projectCount;
        if (allowedPersonnelIds != null)
        {
            projectCount = evaluations.Select(e => e.Assignment.ProjectId).Distinct().Count();
        }
        else
        {
            projectCount = await _context.Projects
                .CountAsync(p => p.CustomerId == customerId && !p.IsDeleted);
        }

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
    /// Puan aralığına göre değerlendirmeler (detay modal için)
    /// </summary>
    [HttpGet("reports/score-range")]
    public async Task<IActionResult> GetEvaluationsByScoreRange(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] decimal minScore,
        [FromQuery] decimal maxScore,
        [FromQuery] bool? isInternal = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        // Supervisor filtrelemesi
        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
        var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

        var start = startDate ?? DateTime.UtcNow.AddMonths(-3);
        var end = endDate ?? DateTime.UtcNow;

        if (start.Kind == DateTimeKind.Unspecified)
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (end.Kind == DateTimeKind.Unspecified)
            end = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.EvaluatedPersonnel)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId == customerId
                && e.CreatedAt >= start
                && e.CreatedAt <= end
                && e.StatusId == EvaluationStatuses.Ids.Completed
                && e.ScorePercentage.HasValue
                && e.ScorePercentage >= minScore
                && e.ScorePercentage < maxScore);

        // İç/Dış dinleme filtresi
        if (isInternal == true)
            query = query.Where(e => e.EvaluatorCustomerPersonnelId != null);
        else if (isInternal == false)
            query = query.Where(e => e.EvaluatorId != null);

        // Supervisor personel filtresi
        if (allowedPersonnelIds != null)
            query = query.Where(e => e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));

        // Supervisor organizasyon filtresi
        if (allowedOrgIds != null)
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && allowedOrgIds.Contains(e.EvaluatedOrganizationId.Value));

        // Project filter
        if (projectIds?.Any() == true)
            query = query.Where(e => projectIds.Contains(e.Assignment.ProjectId));

        // Organization filter
        if (organizationIds?.Any() == true)
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value));

        var evaluations = await query
            .OrderByDescending(e => e.CreatedAt)
            .Take(100)
            .Select(e => new
            {
                evaluationId = e.Id,
                evaluationDate = e.CreatedAt,
                projectName = e.Assignment.Project != null ? e.Assignment.Project.Name : "-",
                personnelName = e.EvaluatedPersonnel != null
                    ? e.EvaluatedPersonnel.FirstName + " " + e.EvaluatedPersonnel.LastName
                    : "-",
                scorePercentage = e.ScorePercentage,
                yellowCards = e.YellowCardCount,
                redCards = e.RedCardCount
            })
            .ToListAsync();

        return Ok(evaluations);
    }

    /// <summary>
    /// Aylık trend raporu (tarih aralığına göre)
    /// </summary>
    [HttpGet("reports/monthly-trend")]
    public async Task<IActionResult> GetReportMonthlyTrend(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] bool? isInternal = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        // Supervisor filtrelemesi
        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
        var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

        var start = startDate ?? DateTime.UtcNow.AddMonths(-6);
        var end = endDate ?? DateTime.UtcNow;

        if (start.Kind == DateTimeKind.Unspecified)
            start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        if (end.Kind == DateTimeKind.Unspecified)
            end = DateTime.SpecifyKind(end.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId == customerId
                && e.CreatedAt >= start
                && e.CreatedAt <= end
                && e.StatusId == EvaluationStatuses.Ids.Completed);

        // İç/Dış dinleme filtresi
        if (isInternal == true)
            query = query.Where(e => e.EvaluatorCustomerPersonnelId != null);
        else if (isInternal == false)
            query = query.Where(e => e.EvaluatorId != null);

        // Supervisor personel filtresi
        if (allowedPersonnelIds != null)
            query = query.Where(e => e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));

        // Supervisor organizasyon filtresi
        if (allowedOrgIds != null)
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && allowedOrgIds.Contains(e.EvaluatedOrganizationId.Value));

        // Project filter
        if (projectIds?.Any() == true)
            query = query.Where(e => projectIds.Contains(e.Assignment.ProjectId));

        // Organization filter
        if (organizationIds?.Any() == true)
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value));

        var evaluations = await query.ToListAsync();

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

        // Supervisor filtrelemesi
        var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

        var query = _context.CustomerOrganizations
            .Where(o => o.CustomerId == customerId && o.IsActive && !o.IsDeleted);

        // Supervisor için sadece yetkili olduğu organizasyonları filtrele
        if (allowedOrgIds != null)
            query = query.Where(o => allowedOrgIds.Contains(o.Id));

        var organizations = await query
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
    /// Organizasyonlar için gelişim trendi (tarih aralığına göre)
    /// Default: Bu haftanın başı (Pazartesi) - Bugün
    /// </summary>
    [HttpGet("organizations/monthly-trend")]
    public async Task<IActionResult> GetOrganizationsMonthlyTrend(
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var now = DateTime.UtcNow;

        // Default: Bu haftanın başı (Pazartesi) - Bugün
        DateTime start;
        if (startDate.HasValue)
        {
            start = startDate.Value.Date;
        }
        else
        {
            // Pazartesi'yi hesapla
            var daysFromMonday = ((int)now.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            start = now.Date.AddDays(-daysFromMonday);
        }

        var end = endDate?.Date ?? now.Date;

        // UTC'ye çevir
        start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
        end = DateTime.SpecifyKind(end.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);

        // Supervisor filtrelemesi
        var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

        // Organizasyonları al
        var organizationsQuery = _context.CustomerOrganizations
            .Where(o => o.CustomerId == customerId && o.IsActive && !o.IsDeleted);

        // Supervisor için sadece yetkili olduğu organizasyonları filtrele
        if (allowedOrgIds != null)
            organizationsQuery = organizationsQuery.Where(o => allowedOrgIds.Contains(o.Id));

        if (organizationIds?.Any() == true)
        {
            organizationsQuery = organizationsQuery.Where(o => organizationIds.Contains(o.Id));
        }

        var organizations = await organizationsQuery
            .OrderBy(o => o.Name)
            .Select(o => new { o.Id, o.Name })
            .ToListAsync();

        // Değerlendirmeleri al (CallDate'e göre - çağrı tarihi)
        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null &&
                        e.Assignment.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.EvaluatedOrganizationId.HasValue &&
                        e.CallDate.HasValue &&
                        e.CallDate.Value >= start &&
                        e.CallDate.Value <= end);

        if (organizationIds?.Any() == true)
        {
            evaluationsQuery = evaluationsQuery.Where(e => e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value));
        }

        var evaluations = await evaluationsQuery.ToListAsync();

        // Tarih aralığına göre gruplama tipini belirle
        var totalDays = (end - start).TotalDays;
        var labels = new List<string>();
        var dateRanges = new List<(DateTime Start, DateTime End)>();

        if (totalDays <= 14)
        {
            // 2 hafta veya daha az: Günlük
            for (var date = start.Date; date <= end.Date; date = date.AddDays(1))
            {
                labels.Add(date.ToString("dd MMM", new System.Globalization.CultureInfo("tr-TR")));
                dateRanges.Add((DateTime.SpecifyKind(date, DateTimeKind.Utc), DateTime.SpecifyKind(date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc)));
            }
        }
        else if (totalDays <= 90)
        {
            // 3 ay veya daha az: Haftalık
            var weekStart = start.Date;
            while (weekStart <= end.Date)
            {
                var weekEnd = weekStart.AddDays(6);
                if (weekEnd > end.Date) weekEnd = end.Date;

                labels.Add(weekStart.ToString("dd MMM", new System.Globalization.CultureInfo("tr-TR")));
                dateRanges.Add((DateTime.SpecifyKind(weekStart, DateTimeKind.Utc), DateTime.SpecifyKind(weekEnd.AddDays(1).AddSeconds(-1), DateTimeKind.Utc)));
                weekStart = weekStart.AddDays(7);
            }
        }
        else
        {
            // 3 aydan fazla: Aylık
            var monthStart = new DateTime(start.Year, start.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            while (monthStart <= end)
            {
                var monthEnd = monthStart.AddMonths(1).AddSeconds(-1);

                labels.Add(monthStart.ToString("MMM yy", new System.Globalization.CultureInfo("tr-TR")));
                dateRanges.Add((monthStart, monthEnd));
                monthStart = monthStart.AddMonths(1);
            }
        }

        // Genel trend (CallDate'e göre)
        var overallTrend = new List<object>();
        foreach (var (rangeStart, rangeEnd) in dateRanges)
        {
            var periodEvals = evaluations.Where(e => e.CallDate.HasValue && e.CallDate.Value >= rangeStart && e.CallDate.Value <= rangeEnd).ToList();
            var withScore = periodEvals.Where(e => e.ScorePercentage.HasValue).ToList();
            var avgScore = withScore.Any() ? withScore.Average(e => (double)e.ScorePercentage!.Value) : 0;

            overallTrend.Add(new
            {
                count = periodEvals.Count,
                averageScore = Math.Round(avgScore, 1)
            });
        }

        // Organizasyon bazlı trend (en fazla 5 organizasyon)
        var topOrganizations = organizations
            .Select(o => new
            {
                o.Id,
                o.Name,
                EvaluationCount = evaluations.Count(e => e.EvaluatedOrganizationId == o.Id)
            })
            .Where(o => o.EvaluationCount > 0)
            .OrderByDescending(o => o.EvaluationCount)
            .Take(5)
            .ToList();

        var organizationTrends = new List<object>();
        foreach (var org in topOrganizations)
        {
            var orgData = new List<double>();
            foreach (var (rangeStart, rangeEnd) in dateRanges)
            {
                var orgEvals = evaluations
                    .Where(e => e.EvaluatedOrganizationId == org.Id &&
                               e.CallDate.HasValue &&
                               e.CallDate.Value >= rangeStart &&
                               e.CallDate.Value <= rangeEnd &&
                               e.ScorePercentage.HasValue)
                    .ToList();

                var avgScore = orgEvals.Any() ? orgEvals.Average(e => (double)e.ScorePercentage!.Value) : 0;
                orgData.Add(Math.Round(avgScore, 1));
            }

            organizationTrends.Add(new
            {
                organizationId = org.Id,
                organizationName = org.Name,
                data = orgData
            });
        }

        return Ok(new
        {
            labels,
            overallTrend,
            organizationTrends,
            periodType = totalDays <= 14 ? "daily" : (totalDays <= 90 ? "weekly" : "monthly"),
            startDate = start,
            endDate = end.Date
        });
    }

    /// <summary>
    /// Süpervizörler (gruplu - organizasyona göre)
    /// Değerlendirme sayısı ve ortalaması: Süpervizörün takımındaki personelin ALDIĞI değerlendirmeler
    /// </summary>
    [HttpGet("supervisors")]
    public async Task<IActionResult> GetSupervisors(
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] string? searchText = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        // Supervisor filtrelemesi
        var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

        // Süpervizör olan personelleri bul (CustomerPersonnelOrganization'da SupervisorId olarak geçenler)
        var supervisorIdsQuery = _context.CustomerPersonnelOrganizations
            .Where(cpo => cpo.SupervisorId.HasValue &&
                          cpo.CustomerOrganization.CustomerId == customerId);

        // Supervisor için sadece yetkili olduğu organizasyonları filtrele
        if (allowedOrgIds != null)
            supervisorIdsQuery = supervisorIdsQuery.Where(cpo => allowedOrgIds.Contains(cpo.CustomerOrganizationId));

        // Organizasyon filtresi
        if (organizationIds?.Any() == true)
        {
            supervisorIdsQuery = supervisorIdsQuery.Where(cpo =>
                organizationIds.Contains(cpo.CustomerOrganizationId));
        }

        var supervisorIds = await supervisorIdsQuery
            .Select(cpo => cpo.SupervisorId!.Value)
            .Distinct()
            .ToListAsync();

        // Her süpervizörün takımındaki personel ID'lerini al (organizasyon filtresine göre)
        var supervisorTeamsQuery = _context.CustomerPersonnelOrganizations
            .Where(cpo => cpo.SupervisorId.HasValue && supervisorIds.Contains(cpo.SupervisorId.Value));

        // Organizasyon filtresi uygula
        if (organizationIds?.Any() == true)
            supervisorTeamsQuery = supervisorTeamsQuery.Where(cpo => organizationIds.Contains(cpo.CustomerOrganizationId));

        var supervisorTeams = await supervisorTeamsQuery
            .GroupBy(cpo => cpo.SupervisorId!.Value)
            .Select(g => new
            {
                SupervisorId = g.Key,
                TeamMemberIds = g.Select(x => x.CustomerPersonnelId).Distinct().ToList()
            })
            .ToListAsync();

        var supervisorTeamDict = supervisorTeams.ToDictionary(x => x.SupervisorId, x => x.TeamMemberIds);

        var supervisorsQuery = _context.CustomerPersonnel
            .Where(cp => supervisorIds.Contains(cp.Id) && cp.IsActive && !cp.IsDeleted);

        // Metin arama filtresi
        if (!string.IsNullOrEmpty(searchText))
        {
            var searchLower = searchText.ToLower();
            supervisorsQuery = supervisorsQuery.Where(cp =>
                (cp.FirstName + " " + cp.LastName).ToLower().Contains(searchLower) ||
                (cp.Title != null && cp.Title.ToLower().Contains(searchLower)));
        }

        // Organizasyon filtresi için liste (boşsa tüm organizasyonlar)
        var orgFilterIds = organizationIds?.Any() == true ? organizationIds : null;

        var supervisors = await supervisorsQuery
            .OrderBy(cp => cp.FirstName).ThenBy(cp => cp.LastName)
            .Select(cp => new
            {
                cp.Id,
                fullName = cp.FirstName + " " + cp.LastName,
                cp.Email,
                cp.Title,
                organizations = _context.CustomerPersonnelOrganizations
                    .Where(cpo => cpo.SupervisorId == cp.Id &&
                                  (orgFilterIds == null || orgFilterIds.Contains(cpo.CustomerOrganizationId)))
                    .Select(cpo => new { cpo.CustomerOrganization.Id, cpo.CustomerOrganization.Name })
                    .Distinct()
                    .ToList(),
                personnelCount = _context.CustomerPersonnelOrganizations
                    .Count(cpo => cpo.SupervisorId == cp.Id &&
                                  (orgFilterIds == null || orgFilterIds.Contains(cpo.CustomerOrganizationId)))
            })
            .ToListAsync();

        // Takım bazlı değerlendirme sayısı ve ortalamasını hesapla (takımın ALDIĞI değerlendirmeler)
        var result = supervisors.Select(s =>
        {
            var teamMemberIds = supervisorTeamDict.ContainsKey(s.Id) ? supervisorTeamDict[s.Id] : new List<int>();

            var evaluationCount = _context.Evaluations
                .Count(e => e.EvaluatedCustomerPersonnelId.HasValue &&
                           teamMemberIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                           e.StatusId == EvaluationStatuses.Ids.Completed);

            var averageScore = _context.Evaluations
                .Where(e => e.EvaluatedCustomerPersonnelId.HasValue &&
                           teamMemberIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                           e.StatusId == EvaluationStatuses.Ids.Completed &&
                           e.ScorePercentage.HasValue)
                .Average(e => (double?)e.ScorePercentage) ?? 0;

            return new
            {
                s.Id,
                s.fullName,
                s.Email,
                s.Title,
                s.organizations,
                s.personnelCount,
                evaluationCount,
                averageScore
            };
        }).ToList();

        // Group by first organization
        var grouped = result
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
    /// Süpervizör için aylık gelişim trendi (takımındaki personelin aldığı değerlendirmeler, son 12 ay)
    /// </summary>
    [HttpGet("supervisors/{supervisorId}/monthly-trend")]
    public async Task<IActionResult> GetSupervisorMonthlyTrend(int supervisorId)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        // Süpervizörü doğrula
        var supervisor = await _context.CustomerPersonnel
            .FirstOrDefaultAsync(cp => cp.Id == supervisorId && cp.CustomerId == customerId && !cp.IsDeleted);

        if (supervisor == null)
            return NotFound(new { message = "Süpervizör bulunamadı" });

        // Süpervizörün takımındaki personel ID'lerini al
        var teamMemberIds = await _context.CustomerPersonnelOrganizations
            .Where(cpo => cpo.SupervisorId == supervisorId)
            .Select(cpo => cpo.CustomerPersonnelId)
            .Distinct()
            .ToListAsync();

        var now = DateTime.UtcNow;
        var startDate = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-11);

        // Takımın aldığı değerlendirmeleri al
        var evaluations = await _context.Evaluations
            .Where(e => e.EvaluatedCustomerPersonnelId.HasValue &&
                        teamMemberIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.CreatedAt >= startDate)
            .ToListAsync();

        // Aylık trend verisi oluştur
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

        return Ok(new
        {
            supervisorId,
            supervisorName = supervisor.FirstName + " " + supervisor.LastName,
            teamMemberCount = teamMemberIds.Count,
            monthlyTrend = monthlyData
        });
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
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] List<string>? evaluatorNames = null,
        [FromQuery] List<string>? personnelNames = null,
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] List<string>? callIds = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.EvaluatorCustomerPersonnel)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Include(e => e.CustomerDealer)
            .Where(e => e.Assignment.Project.CustomerId == customerId &&
                       e.EvaluatorCustomerPersonnelId != null); // Taslaklar dahil

        // Rol bazlı filtreleme (İç Dinlemeler)
        // Operator: Sadece kendisinin değerlendirildiği kayıtlar
        // Supervisor: Kendi yaptığı değerlendirmeler + takımındaki personelin değerlendirildiği kayıtlar
        if (role == "CustomerOperator" && personnelId.HasValue)
        {
            query = query.Where(e => e.EvaluatedCustomerPersonnelId == personnelId.Value);
        }
        else if (role == "CustomerSupervisor" && personnelId.HasValue)
        {
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null)
            {
                query = query.Where(e =>
                    e.EvaluatorCustomerPersonnelId == personnelId.Value ||
                    (e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value)));
            }
        }
        // Manager ve Admin: Tüm kayıtları görür

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
        if (projectIds?.Any() == true)
        {
            query = query.Where(e => projectIds.Contains(e.Assignment.ProjectId));
        }

        // Evaluator name filter
        if (evaluatorNames?.Any() == true)
        {
            var lowerNames = evaluatorNames.Select(n => n.ToLower()).ToList();
            query = query.Where(e => e.EvaluatorCustomerPersonnel != null &&
                lowerNames.Any(n => (e.EvaluatorCustomerPersonnel.FirstName + " " + e.EvaluatorCustomerPersonnel.LastName).ToLower().Contains(n)));
        }

        // Personnel name filter
        if (personnelNames?.Any() == true)
        {
            var lowerNames = personnelNames.Select(n => n.ToLower()).ToList();
            query = query.Where(e =>
                lowerNames.Any(n =>
                    (e.EvaluatedCustomerPersonnel != null && (e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName).ToLower().Contains(n)) ||
                    (e.EvaluatedUnknownPersonnel != null && e.EvaluatedUnknownPersonnel.ToLower().Contains(n))));
        }

        // Organization filter
        if (organizationIds?.Any() == true)
        {
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value));
        }

        // CallId filter
        if (callIds?.Any() == true)
        {
            var lowerCallIds = callIds.Select(c => c.ToLower()).ToList();
            query = query.Where(e => e.CallId != null && lowerCallIds.Any(c => e.CallId.ToLower().Contains(c)));
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
                projectCode = e.Assignment.Project.Code,
                evaluatorName = e.EvaluatorCustomerPersonnel != null ? e.EvaluatorCustomerPersonnel.FirstName + " " + e.EvaluatorCustomerPersonnel.LastName : null,
                evaluatedPersonnelName = e.EvaluatedCustomerPersonnel != null ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName : e.EvaluatedUnknownPersonnel,
                dealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : (string?)null,
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
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] List<string>? personnelNames = null,
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] List<string>? callIds = null,
        [FromQuery] decimal? minScore = null,
        [FromQuery] decimal? maxScore = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Evaluator)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.EvaluatedOrganization)
            .Include(e => e.CustomerDealer)
            .Where(e => e.Assignment.Project.CustomerId == customerId &&
                       e.EvaluatorId != null &&
                       e.StatusId == EvaluationStatuses.Ids.Completed);

        // Rol bazlı filtreleme (Dış Dinlemeler)
        // Operator: Sadece kendisinin değerlendirildiği kayıtlar
        // Supervisor: Takımındaki personelin değerlendirildiği kayıtlar
        if (role == "CustomerOperator" && personnelId.HasValue)
        {
            query = query.Where(e => e.EvaluatedCustomerPersonnelId == personnelId.Value);
        }
        else if (role == "CustomerSupervisor" && personnelId.HasValue)
        {
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null)
            {
                query = query.Where(e =>
                    e.EvaluatedCustomerPersonnelId.HasValue && allowedPersonnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value));
            }
        }
        // Manager ve Admin: Tüm kayıtları görür

        // Date filters (ControlDate ziyaret tarihi, CallDate çağrı tarihi)
        if (startDate.HasValue)
        {
            var start = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => (e.ControlDate ?? e.CallDate ?? e.CompletedAt ?? e.CreatedAt) >= start);
        }
        if (endDate.HasValue)
        {
            var end = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddSeconds(-1), DateTimeKind.Utc);
            query = query.Where(e => (e.ControlDate ?? e.CallDate ?? e.CompletedAt ?? e.CreatedAt) <= end);
        }

        // Project filter
        if (projectIds?.Any() == true)
        {
            query = query.Where(e => projectIds.Contains(e.Assignment.ProjectId));
        }

        // Personnel name filter
        if (personnelNames?.Any() == true)
        {
            var lowerNames = personnelNames.Select(n => n.ToLower()).ToList();
            query = query.Where(e =>
                lowerNames.Any(n =>
                    (e.EvaluatedCustomerPersonnel != null && (e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName).ToLower().Contains(n)) ||
                    (e.EvaluatedUnknownPersonnel != null && e.EvaluatedUnknownPersonnel.ToLower().Contains(n))));
        }

        // Organization filter
        if (organizationIds?.Any() == true)
        {
            query = query.Where(e => e.EvaluatedOrganizationId.HasValue && organizationIds.Contains(e.EvaluatedOrganizationId.Value));
        }

        // CallId filter
        if (callIds?.Any() == true)
        {
            var lowerCallIds = callIds.Select(c => c.ToLower()).ToList();
            query = query.Where(e => e.CallId != null && lowerCallIds.Any(c => e.CallId.ToLower().Contains(c)));
        }

        // Score range filter
        if (minScore.HasValue)
        {
            query = query.Where(e => e.ScorePercentage.HasValue && e.ScorePercentage.Value >= minScore.Value);
        }
        if (maxScore.HasValue)
        {
            query = query.Where(e => e.ScorePercentage.HasValue && e.ScorePercentage.Value <= maxScore.Value);
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
            .OrderByDescending(e => e.ControlDate ?? e.CallDate ?? e.CompletedAt ?? e.CreatedAt)
            .Skip(((page ?? 1) - 1) * (pageSize ?? 20))
            .Take(pageSize ?? 20)
            .Select(e => new
            {
                e.Id,
                evaluationDate = e.CompletedAt ?? e.CreatedAt,
                projectName = e.Assignment.Project.Name,
                projectTypeId = e.Assignment.Project.ProjectTypeId,
                evaluatedPersonnelName = e.EvaluatedCustomerPersonnel != null ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName : e.EvaluatedUnknownPersonnel,
                dealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : (string?)null,
                organizationName = e.EvaluatedOrganization != null ? e.EvaluatedOrganization.Name : null,
                e.TotalScore,
                e.ScorePercentage,
                e.YellowCardCount,
                e.RedCardCount,
                e.CallId,
                e.CallDate,
                e.CallTime,
                e.Duration,
                e.ControlDate,
                e.ControlTime
            })
            .ToListAsync();

        return Ok(new { items = evaluations, total, page = page ?? 1, pageSize = pageSize ?? 20, averageScore = Math.Round(averageScore, 1) });
    }

    /// <summary>
    /// Değerlendirme detayını getirir (CustomerPortal)
    /// </summary>
    [HttpGet("evaluations/{evaluationId}")]
    public async Task<IActionResult> GetEvaluationDetail(int evaluationId)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var evaluation = await _context.Evaluations
                .Include(e => e.Assignment)
                    .ThenInclude(a => a!.Project)
                .FirstOrDefaultAsync(e => e.Id == evaluationId);

            if (evaluation?.Assignment?.Project?.CustomerId != customerId)
                return Forbid();

            // Supervisor erişim kontrolü
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null && evaluation.EvaluatedCustomerPersonnelId.HasValue &&
                !allowedPersonnelIds.Contains(evaluation.EvaluatedCustomerPersonnelId.Value))
                return StatusCode(403, new { message = "Bu değerlendirmeyi görüntüleme yetkiniz bulunmamaktadır." });

            var detail = await _reportService.GetEvaluationDetailAsync(evaluationId);
            if (detail == null)
                return NotFound(new { message = "Değerlendirme bulunamadı." });

            return Ok(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error getting evaluation detail {EvaluationId} for customer {CustomerId}", evaluationId, customerId);
            return StatusCode(500, new { message = "Değerlendirme detayı yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Değerlendirme ekli dosyalarını getirir (CustomerPortal)
    /// </summary>
    [HttpGet("evaluations/{evaluationId}/attachments")]
    public async Task<IActionResult> GetEvaluationAttachments(int evaluationId)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var evaluation = await _context.Evaluations
                .Include(e => e.Assignment)
                    .ThenInclude(a => a!.Project)
                .FirstOrDefaultAsync(e => e.Id == evaluationId);

            if (evaluation?.Assignment?.Project?.CustomerId != customerId)
                return Forbid();

            // Supervisor erişim kontrolü
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null && evaluation.EvaluatedCustomerPersonnelId.HasValue &&
                !allowedPersonnelIds.Contains(evaluation.EvaluatedCustomerPersonnelId.Value))
                return StatusCode(403, new { message = "Bu değerlendirmenin dosyalarını görüntüleme yetkiniz bulunmamaktadır." });

            var attachments = await _context.EvaluationAttachments
                .Where(a => a.EvaluationId == evaluationId && !a.IsDeleted)
                .Select(a => new
                {
                    id = a.Id,
                    fileName = a.FileName,
                    fileSize = a.FileSize,
                    contentType = a.ContentType,
                    uploadedAt = a.CreatedAt
                })
                .ToListAsync();

            return Ok(attachments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error getting evaluation attachments {EvaluationId} for customer {CustomerId}", evaluationId, customerId);
            return StatusCode(500, new { message = "Dosyalar yüklenirken hata oluştu." });
        }
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

            // Supervisor erişim kontrolü
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null && evaluation.EvaluatedCustomerPersonnelId.HasValue &&
                !allowedPersonnelIds.Contains(evaluation.EvaluatedCustomerPersonnelId.Value))
                return StatusCode(403, new { message = "Bu değerlendirmeyi dışa aktarma yetkiniz bulunmamaktadır." });

            var result = await _reportService.ExportEvaluationDetailToExcelAsync(evaluationId, excludeEvaluatorInfo: true);
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
            filter.ProjectCustomerIds = new List<int> { customerId.Value };

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
            filter.ProjectCustomerIds = new List<int> { customerId.Value };

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
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] List<string>? penaltyTypes,
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
            // Supervisor filtrelemesi
            var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

            // Supervisor için organizasyon kısıtlaması
            var effectiveOrgIds = organizationIds;
            if (allowedOrgIds != null)
            {
                if (organizationIds?.Any() == true)
                    effectiveOrgIds = organizationIds.Where(id => allowedOrgIds.Contains(id)).ToList();
                else
                    effectiveOrgIds = allowedOrgIds;
            }

            var filter = new PenaltyFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value }, // Otomatik müşteri filtresi
                OrganizationIds = effectiveOrgIds,
                ChecklistIds = checklistIds,
                PenaltyTypes = penaltyTypes,
                Page = page,
                PageSize = pageSize
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

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
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] List<string>? penaltyTypes,
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
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value },
                OrganizationIds = organizationIds,
                ChecklistIds = checklistIds,
                PenaltyTypes = penaltyTypes,
                Page = 1,
                PageSize = int.MaxValue
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

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
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] List<int>? checklistIds,
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
            // Supervisor filtrelemesi
            var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

            // Supervisor için organizasyon kısıtlaması
            var effectiveOrgIds = organizationIds;
            if (allowedOrgIds != null)
            {
                if (organizationIds?.Any() == true)
                    effectiveOrgIds = organizationIds.Where(id => allowedOrgIds.Contains(id)).ToList();
                else
                    effectiveOrgIds = allowedOrgIds;
            }

            var filter = new SuggestionsFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value }, // Otomatik müşteri filtresi
                OrganizationIds = effectiveOrgIds,
                ChecklistIds = checklistIds,
                SearchText = searchText,
                Page = page,
                PageSize = pageSize
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

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
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? checklistIds,
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
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value },
                ChecklistIds = checklistIds
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

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
    /// En çok seçilen alt kriterler (CustomerPortal)
    /// </summary>
    [HttpGet("reports/suggestions/top-subcriteria")]
    public async Task<IActionResult> GetTopSubCriteria(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? checklistIds,
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
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value },
                ChecklistIds = checklistIds
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.GetTopSubCriteriaAsync(filter, top);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading top subcriteria for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "En çok seçilen alt kriterler yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// En çok öneri yazılan sorular Excel Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/suggestions/top-questions/export")]
    public async Task<IActionResult> ExportTopSuggestedQuestionsToExcel(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int top = 100)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var filter = new SuggestionsFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value },
                ChecklistIds = checklistIds
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var data = await _reportService.GetTopSuggestedQuestionsAsync(filter, top);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("En Çok Önerilen Sorular");

            // Headers
            worksheet.Cell(1, 1).Value = "Soru";
            worksheet.Cell(1, 2).Value = "Checklist";
            worksheet.Cell(1, 3).Value = "Grup";
            worksheet.Cell(1, 4).Value = "Öneri Sayısı";
            worksheet.Cell(1, 5).Value = "Değerlendirme Sayısı";
            worksheet.Cell(1, 6).Value = "Ortalama Puan (%)";

            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Data rows
            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.QuestionText;
                worksheet.Cell(row, 2).Value = item.ChecklistName;
                worksheet.Cell(row, 3).Value = item.GroupName;
                worksheet.Cell(row, 4).Value = item.SuggestionCount;
                worksheet.Cell(row, 5).Value = item.EvaluationCount;
                worksheet.Cell(row, 6).Value = Math.Round(item.AverageScore, 1);
                row++;
            }

            worksheet.Columns().AdjustToContents();
            ExcelHelper.ApplyLongTextColumnStyles(worksheet);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"EnCokOnerilenSorular_{DateTime.Now:yyyyMMdd}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting top suggested questions for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "En çok önerilen sorular export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Öneriler Raporu Excel Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/suggestions/export")]
    public async Task<IActionResult> ExportSuggestionsToExcel(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? checklistIds,
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
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value },
                ChecklistIds = checklistIds,
                SearchText = searchText,
                Page = 1,
                PageSize = int.MaxValue
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

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
    public async Task<IActionResult> GetPersonnelList([FromQuery] List<int>? organizationIds = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

            // Geriye uyumluluk: tekil organizationId parametresi için array'in ilk elemanını kullan
            var organizationId = organizationIds?.FirstOrDefault();
            var personnel = await _reportService.GetEvaluatedPersonnelListAsync(customerId.Value, organizationId);

            // Supervisor için sadece yetkili olduğu personeli filtrele
            if (allowedPersonnelIds != null)
            {
                personnel = personnel.Where(p => allowedPersonnelIds.Contains(p.Id));
            }

            // Supervisor için sadece yetkili olduğu organizasyonları filtrele
            if (allowedOrgIds != null)
            {
                personnel = personnel.Where(p => p.OrganizationId.HasValue && allowedOrgIds.Contains(p.OrganizationId.Value));
            }

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
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Güvenlik kontrolü: Personelin bu müşteriye ait olup olmadığını doğrula
            var personnel = await _context.CustomerPersonnel.FindAsync(personnelId);
            if (personnel == null || personnel.CustomerId != customerId.Value)
                return NotFound(new { message = "Temsilci bulunamadı." });

            // Supervisor erişim kontrolü
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null && !allowedPersonnelIds.Contains(personnelId))
                return StatusCode(403, new { message = "Bu temsilcinin karnesini görüntüleme yetkiniz bulunmamaktadır." });

            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds,
                DateRanges = dateRanges
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
            return StatusCode(500, new { message = "Temsilci karnesi yüklenirken hata oluştu.", error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Kendi Karnem - CustomerOperator'ın kendi performans karnesini görüntüler
    /// Token'daki NameIdentifier (CustomerPersonnelId) ile kendi karnesini döner
    /// </summary>
    [HttpGet("reports/my-report-card")]
    public async Task<IActionResult> GetMyReportCard(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Token'dan CustomerPersonnelId'yi al
            var personnelIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;

            if (userType != "CustomerPersonnel" || string.IsNullOrEmpty(personnelIdClaim) || !int.TryParse(personnelIdClaim, out var personnelId))
            {
                return BadRequest(new { message = "Bu endpoint sadece müşteri personeli için kullanılabilir." });
            }

            // Personelin bu müşteriye ait olup olmadığını doğrula
            var personnel = await _context.CustomerPersonnel.FindAsync(personnelId);
            if (personnel == null || personnel.CustomerId != customerId.Value)
                return NotFound(new { message = "Personel bulunamadı." });

            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds,
                DateRanges = dateRanges
            };

            var result = await _reportService.GetPersonnelReportCardAsync(filter);

            if (result == null)
                return NotFound(new { message = "Karne verisi bulunamadı." });

            // EvaluatorName alanlarını temizle (temsilci görmemeli)
            foreach (var evaluation in result.RecentEvaluations)
            {
                evaluation.EvaluatorName = null;
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading my report card for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Karne yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Temsilci Karnesi Excel Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/personnel-report-card/{personnelId}/export")]
    public async Task<IActionResult> ExportPersonnelReportCard(
        int personnelId,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Güvenlik kontrolü: Personelin bu müşteriye ait olup olmadığını doğrula
            var personnel = await _context.CustomerPersonnel.FindAsync(personnelId);
            if (personnel == null || personnel.CustomerId != customerId.Value)
                return NotFound(new { message = "Temsilci bulunamadı." });

            // Supervisor erişim kontrolü
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null && !allowedPersonnelIds.Contains(personnelId))
                return StatusCode(403, new { message = "Bu temsilcinin karnesini görüntüleme yetkiniz bulunmamaktadır." });

            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds,
                DateRanges = dateRanges
            };

            var result = await _reportService.ExportPersonnelReportCardToExcelAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting personnel report card for customer {CustomerId}, personnel {PersonnelId}", customerId, personnelId);
            return StatusCode(500, new { message = "Temsilci karnesi export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Temsilci Karnesi Word Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/personnel-report-card/{personnelId}/export-word")]
    public async Task<IActionResult> ExportPersonnelReportCardToWord(
        int personnelId,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Personelin bu müşteriye ait olup olmadığını kontrol et
            var personnel = await _context.CustomerPersonnel
                .FirstOrDefaultAsync(p => p.Id == personnelId && p.CustomerId == customerId);

            if (personnel == null)
                return NotFound(new { message = "Personel bulunamadı." });

            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds,
                DateRanges = dateRanges
            };

            var result = await _reportService.ExportPersonnelReportCardToWordAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting personnel report card to Word for customer {CustomerId}, personnel {PersonnelId}", customerId, personnelId);
            return StatusCode(500, new { message = "Temsilci karnesi Word export edilirken hata oluştu." });
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
            filter.ProjectCustomerIds = new List<int> { customerId.Value };

            var result = await _reportService.ExportProjectPerformanceReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting project performance report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Rapor oluşturulurken hata oluştu." });
        }
    }

    /// <summary>
    /// Dönemlere Göre Personel Başarı Tablosu (CustomerPortal)
    /// </summary>
    [HttpGet("reports/performance-by-period")]
    public async Task<IActionResult> GetPerformanceByPeriod(
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

            // Müşteriye ait projeleri al
            var projectsQuery = _context.Projects
                .Where(p => p.CustomerId == customerId && p.IsActive && !p.IsDeleted);

            if (projectIds?.Any() == true)
            {
                projectsQuery = projectsQuery.Where(p => projectIds.Contains(p.Id));
            }

            var filteredProjectIds = await projectsQuery.Select(p => p.Id).ToListAsync();

            if (!filteredProjectIds.Any())
            {
                return Ok(new { periods = new List<object>(), data = new List<object>() });
            }

            // Personelleri al (organizasyon filtresiyle)
            var personnelQuery = _context.CustomerPersonnel
                .Include(cp => cp.OrganizationAssignments)
                    .ThenInclude(cpo => cpo.CustomerOrganization)
                .Where(cp => cp.CustomerId == customerId && cp.IsActive && !cp.IsDeleted);

            // Supervisor personel filtresi
            if (allowedPersonnelIds != null)
            {
                personnelQuery = personnelQuery.Where(cp => allowedPersonnelIds.Contains(cp.Id));
            }

            // Supervisor organizasyon filtresi
            if (allowedOrgIds != null)
            {
                personnelQuery = personnelQuery.Where(cp =>
                    cp.OrganizationAssignments.Any(cpo => allowedOrgIds.Contains(cpo.CustomerOrganizationId)));
            }

            // Kullanıcının seçtiği organizasyon filtresi
            if (organizationIds?.Any() == true)
            {
                personnelQuery = personnelQuery.Where(cp =>
                    cp.OrganizationAssignments.Any(cpo => organizationIds.Contains(cpo.CustomerOrganizationId)));
            }

            var personnel = await personnelQuery
                .OrderBy(cp => cp.FirstName).ThenBy(cp => cp.LastName)
                .Select(cp => new
                {
                    cp.Id,
                    FullName = cp.FirstName + " " + cp.LastName,
                    OrganizationName = cp.OrganizationAssignments
                        .Select(cpo => cpo.CustomerOrganization.Name)
                        .FirstOrDefault() ?? "-"
                })
                .ToListAsync();

            var personnelIds = personnel.Select(p => p.Id).ToList();

            // Önce AssignmentPeriod'ları kontrol et
            var assignmentPeriodsQuery = _context.AssignmentPeriods
                .Include(ap => ap.Assignment)
                    .ThenInclude(a => a.Project)
                .Where(ap => filteredProjectIds.Contains(ap.Assignment.ProjectId) && !ap.IsDeleted);

            // Tarih filtresi (AssignmentPeriod'lar için dönem tarihlerine göre)
            if (startDate.HasValue)
            {
                assignmentPeriodsQuery = assignmentPeriodsQuery.Where(ap => ap.EndDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                assignmentPeriodsQuery = assignmentPeriodsQuery.Where(ap => ap.StartDate <= endDate.Value);
            }

            var assignmentPeriods = await assignmentPeriodsQuery
                .OrderBy(ap => ap.StartDate)
                .Select(ap => new
                {
                    ap.Id,
                    ap.Name,
                    ap.StartDate,
                    ap.EndDate,
                    ProjectName = ap.Assignment.Project.Name
                })
                .ToListAsync();

            // AssignmentPeriod varsa onları kullan
            if (assignmentPeriods.Any())
            {
                var periodIds = assignmentPeriods.Select(p => p.Id).ToList();

                var evaluations = await _context.Evaluations
                    .Where(e => e.AssignmentPeriodId.HasValue &&
                               periodIds.Contains(e.AssignmentPeriodId.Value) &&
                               e.EvaluatedCustomerPersonnelId.HasValue &&
                               personnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                               e.StatusId == EvaluationStatuses.Ids.Completed &&
                               e.ScorePercentage.HasValue)
                    .Select(e => new
                    {
                        e.AssignmentPeriodId,
                        e.EvaluatedCustomerPersonnelId,
                        e.ScorePercentage,
                        e.YellowCardCount,
                        e.RedCardCount
                    })
                    .ToListAsync();

                var data = personnel.Select(p => new
                {
                    personnelId = p.Id,
                    personnelName = p.FullName,
                    organizationName = p.OrganizationName,
                    periodScores = assignmentPeriods.Select(period =>
                    {
                        var periodEvals = evaluations
                            .Where(e => e.AssignmentPeriodId == period.Id && e.EvaluatedCustomerPersonnelId == p.Id)
                            .ToList();

                        return new
                        {
                            periodId = period.Id,
                            periodName = period.Name,
                            evaluationCount = periodEvals.Count,
                            averageScore = periodEvals.Any() ? Math.Round(periodEvals.Average(e => (double)e.ScorePercentage!.Value), 1) : (double?)null,
                            yellowCardCount = periodEvals.Sum(e => e.YellowCardCount),
                            redCardCount = periodEvals.Sum(e => e.RedCardCount)
                        };
                    }).ToList(),
                    overallAverage = evaluations
                        .Where(e => e.EvaluatedCustomerPersonnelId == p.Id)
                        .Select(e => (double)e.ScorePercentage!.Value)
                        .DefaultIfEmpty()
                        .Average(),
                    totalEvaluations = evaluations.Count(e => e.EvaluatedCustomerPersonnelId == p.Id)
                })
                .Where(p => p.totalEvaluations > 0)
                .OrderByDescending(p => p.overallAverage)
                .ToList();

                return Ok(new
                {
                    periods = assignmentPeriods.Select(p => new { p.Id, p.Name, p.ProjectName, p.StartDate, p.EndDate }),
                    data
                });
            }

            // AssignmentPeriod yoksa CallDate'e göre aylık dönemler oluştur
            var allEvaluationsQuery = _context.Evaluations
                .Include(e => e.Assignment)
                .Where(e => e.Assignment != null &&
                           filteredProjectIds.Contains(e.Assignment.ProjectId) &&
                           e.EvaluatedCustomerPersonnelId.HasValue &&
                           personnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                           e.StatusId == EvaluationStatuses.Ids.Completed &&
                           e.ScorePercentage.HasValue &&
                           e.CallDate.HasValue);

            // Tarih filtresi (CallDate'e göre)
            if (startDate.HasValue)
            {
                allEvaluationsQuery = allEvaluationsQuery.Where(e => e.CallDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                allEvaluationsQuery = allEvaluationsQuery.Where(e => e.CallDate <= endDate.Value);
            }

            var allEvaluations = await allEvaluationsQuery
                .Select(e => new
                {
                    e.EvaluatedCustomerPersonnelId,
                    e.ScorePercentage,
                    e.YellowCardCount,
                    e.RedCardCount,
                    e.CallDate,
                    ProjectName = e.Assignment!.Project!.Name
                })
                .ToListAsync();

            if (!allEvaluations.Any())
            {
                return Ok(new { periods = new List<object>(), data = new List<object>() });
            }

            // CallDate'e göre aylık dönemler oluştur
            var monthlyPeriods = allEvaluations
                .GroupBy(e => new { Year = e.CallDate!.Value.Year, Month = e.CallDate!.Value.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select((g, idx) => new
                {
                    Id = -(idx + 1), // Negatif ID (sanal dönem)
                    Name = $"{g.Key.Year}-{g.Key.Month:D2}",
                    StartDate = new DateTime(g.Key.Year, g.Key.Month, 1),
                    EndDate = new DateTime(g.Key.Year, g.Key.Month, DateTime.DaysInMonth(g.Key.Year, g.Key.Month)),
                    ProjectName = g.Select(e => e.ProjectName).FirstOrDefault() ?? "-",
                    Evaluations = g.ToList()
                })
                .ToList();

            var monthlyData = personnel.Select(p => new
            {
                personnelId = p.Id,
                personnelName = p.FullName,
                organizationName = p.OrganizationName,
                periodScores = monthlyPeriods.Select(period =>
                {
                    var periodEvals = period.Evaluations
                        .Where(e => e.EvaluatedCustomerPersonnelId == p.Id)
                        .ToList();

                    return new
                    {
                        periodId = period.Id,
                        periodName = period.Name,
                        evaluationCount = periodEvals.Count,
                        averageScore = periodEvals.Any() ? Math.Round(periodEvals.Average(e => (double)e.ScorePercentage!.Value), 1) : (double?)null,
                        yellowCardCount = periodEvals.Sum(e => e.YellowCardCount),
                        redCardCount = periodEvals.Sum(e => e.RedCardCount)
                    };
                }).ToList(),
                overallAverage = allEvaluations
                    .Where(e => e.EvaluatedCustomerPersonnelId == p.Id)
                    .Select(e => (double)e.ScorePercentage!.Value)
                    .DefaultIfEmpty()
                    .Average(),
                totalEvaluations = allEvaluations.Count(e => e.EvaluatedCustomerPersonnelId == p.Id)
            })
            .Where(p => p.totalEvaluations > 0)
            .OrderByDescending(p => p.overallAverage)
            .ToList();

            return Ok(new
            {
                periods = monthlyPeriods.Select(p => new { p.Id, p.Name, p.ProjectName, p.StartDate, p.EndDate }),
                data = monthlyData
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading performance by period for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Rapor yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Dönemlere Göre Personel Başarı Tablosu - Excel Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/performance-by-period/export")]
    public async Task<IActionResult> ExportPerformanceByPeriod(
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

            // Proje filtresi
            var projectsQuery = _context.Projects
                .Where(p => p.CustomerId == customerId && p.IsActive && !p.IsDeleted);

            if (projectIds?.Any() == true)
            {
                projectsQuery = projectsQuery.Where(p => projectIds.Contains(p.Id));
            }

            var filteredProjectIds = await projectsQuery.Select(p => p.Id).ToListAsync();

            // Personel filtresi
            var personnelQuery = _context.CustomerPersonnel
                .Include(cp => cp.OrganizationAssignments)
                    .ThenInclude(cpo => cpo.CustomerOrganization)
                .Where(cp => cp.CustomerId == customerId && cp.IsActive && !cp.IsDeleted);

            // Supervisor personel filtresi
            if (allowedPersonnelIds != null)
            {
                personnelQuery = personnelQuery.Where(cp => allowedPersonnelIds.Contains(cp.Id));
            }

            // Supervisor organizasyon filtresi
            if (allowedOrgIds != null)
            {
                personnelQuery = personnelQuery.Where(cp =>
                    cp.OrganizationAssignments.Any(cpo => allowedOrgIds.Contains(cpo.CustomerOrganizationId)));
            }

            // Kullanıcının seçtiği organizasyon filtresi
            if (organizationIds?.Any() == true)
            {
                personnelQuery = personnelQuery.Where(cp =>
                    cp.OrganizationAssignments.Any(cpo => organizationIds.Contains(cpo.CustomerOrganizationId)));
            }

            var personnel = await personnelQuery
                .OrderBy(cp => cp.FirstName).ThenBy(cp => cp.LastName)
                .Select(cp => new
                {
                    cp.Id,
                    FullName = cp.FirstName + " " + cp.LastName,
                    OrganizationName = cp.OrganizationAssignments
                        .Select(cpo => cpo.CustomerOrganization.Name)
                        .FirstOrDefault() ?? "-"
                })
                .ToListAsync();

            var personnelIds = personnel.Select(p => p.Id).ToList();

            // AssignmentPeriod kontrolü
            var assignmentPeriodsQuery = _context.AssignmentPeriods
                .Include(ap => ap.Assignment)
                    .ThenInclude(a => a.Project)
                .Where(ap => filteredProjectIds.Contains(ap.Assignment.ProjectId) && !ap.IsDeleted);

            // Tarih filtresi (AssignmentPeriod'lar için dönem tarihlerine göre)
            if (startDate.HasValue)
            {
                assignmentPeriodsQuery = assignmentPeriodsQuery.Where(ap => ap.EndDate >= startDate.Value);
            }
            if (endDate.HasValue)
            {
                assignmentPeriodsQuery = assignmentPeriodsQuery.Where(ap => ap.StartDate <= endDate.Value);
            }

            var assignmentPeriods = await assignmentPeriodsQuery
                .OrderBy(ap => ap.StartDate)
                .Select(ap => new { ap.Id, ap.Name })
                .ToListAsync();

            // Excel oluştur
            using var workbook = new ClosedXML.Excel.XLWorkbook();
            var sheet = workbook.Worksheets.Add("Dönem Bazlı Başarı");

            // AssignmentPeriod varsa onu kullan
            if (assignmentPeriods.Any())
            {
                var periodIds = assignmentPeriods.Select(p => p.Id).ToList();

                var evaluations = await _context.Evaluations
                    .Where(e => e.AssignmentPeriodId.HasValue &&
                               periodIds.Contains(e.AssignmentPeriodId.Value) &&
                               e.EvaluatedCustomerPersonnelId.HasValue &&
                               personnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                               e.StatusId == EvaluationStatuses.Ids.Completed &&
                               e.ScorePercentage.HasValue)
                    .Select(e => new
                    {
                        e.AssignmentPeriodId,
                        e.EvaluatedCustomerPersonnelId,
                        e.ScorePercentage
                    })
                    .ToListAsync();

                // Headers
                sheet.Cell(1, 1).Value = "Personel";
                sheet.Cell(1, 1).Style.Font.Bold = true;
                sheet.Cell(1, 2).Value = "Organizasyon";
                sheet.Cell(1, 2).Style.Font.Bold = true;

                int col = 3;
                foreach (var period in assignmentPeriods)
                {
                    sheet.Cell(1, col).Value = period.Name;
                    sheet.Cell(1, col).Style.Font.Bold = true;
                    col++;
                }
                sheet.Cell(1, col).Value = "Genel Ortalama";
                sheet.Cell(1, col).Style.Font.Bold = true;
                sheet.Cell(1, col + 1).Value = "Toplam Değerlendirme";
                sheet.Cell(1, col + 1).Style.Font.Bold = true;

                // Data rows
                int row = 2;
                foreach (var p in personnel)
                {
                    var personEvals = evaluations.Where(e => e.EvaluatedCustomerPersonnelId == p.Id).ToList();
                    if (!personEvals.Any()) continue;

                    sheet.Cell(row, 1).Value = p.FullName;
                    sheet.Cell(row, 2).Value = p.OrganizationName;

                    col = 3;
                    foreach (var period in assignmentPeriods)
                    {
                        var periodEvals = personEvals.Where(e => e.AssignmentPeriodId == period.Id).ToList();
                        if (periodEvals.Any())
                        {
                            var avg = periodEvals.Average(e => (double)e.ScorePercentage!.Value);
                            sheet.Cell(row, col).Value = Math.Round(avg, 1);
                            if (avg >= 80)
                                sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGreen;
                            else if (avg >= 60)
                                sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightYellow;
                            else
                                sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightCoral;
                        }
                        else
                        {
                            sheet.Cell(row, col).Value = "-";
                        }
                        col++;
                    }

                    var overallAvg = personEvals.Average(e => (double)e.ScorePercentage!.Value);
                    sheet.Cell(row, col).Value = Math.Round(overallAvg, 1);
                    sheet.Cell(row, col).Style.Font.Bold = true;
                    sheet.Cell(row, col + 1).Value = personEvals.Count;

                    row++;
                }
            }
            else
            {
                // AssignmentPeriod yoksa CallDate'e göre aylık dönemler oluştur
                var allEvaluationsQuery = _context.Evaluations
                    .Include(e => e.Assignment)
                    .Where(e => e.Assignment != null &&
                               filteredProjectIds.Contains(e.Assignment.ProjectId) &&
                               e.EvaluatedCustomerPersonnelId.HasValue &&
                               personnelIds.Contains(e.EvaluatedCustomerPersonnelId.Value) &&
                               e.StatusId == EvaluationStatuses.Ids.Completed &&
                               e.ScorePercentage.HasValue &&
                               e.CallDate.HasValue);

                // Tarih filtresi (CallDate'e göre)
                if (startDate.HasValue)
                {
                    allEvaluationsQuery = allEvaluationsQuery.Where(e => e.CallDate >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    allEvaluationsQuery = allEvaluationsQuery.Where(e => e.CallDate <= endDate.Value);
                }

                var allEvaluations = await allEvaluationsQuery
                    .Select(e => new
                    {
                        e.EvaluatedCustomerPersonnelId,
                        e.ScorePercentage,
                        e.CallDate
                    })
                    .ToListAsync();

                // CallDate'e göre aylık dönemler oluştur
                var monthlyPeriods = allEvaluations
                    .GroupBy(e => new { Year = e.CallDate!.Value.Year, Month = e.CallDate!.Value.Month })
                    .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                    .Select(g => new
                    {
                        Name = $"{g.Key.Year}-{g.Key.Month:D2}",
                        Year = g.Key.Year,
                        Month = g.Key.Month
                    })
                    .ToList();

                // Headers
                sheet.Cell(1, 1).Value = "Personel";
                sheet.Cell(1, 1).Style.Font.Bold = true;
                sheet.Cell(1, 2).Value = "Organizasyon";
                sheet.Cell(1, 2).Style.Font.Bold = true;

                int col = 3;
                foreach (var period in monthlyPeriods)
                {
                    sheet.Cell(1, col).Value = period.Name;
                    sheet.Cell(1, col).Style.Font.Bold = true;
                    col++;
                }
                sheet.Cell(1, col).Value = "Genel Ortalama";
                sheet.Cell(1, col).Style.Font.Bold = true;
                sheet.Cell(1, col + 1).Value = "Toplam Değerlendirme";
                sheet.Cell(1, col + 1).Style.Font.Bold = true;

                // Data rows
                int row = 2;
                foreach (var p in personnel)
                {
                    var personEvals = allEvaluations.Where(e => e.EvaluatedCustomerPersonnelId == p.Id).ToList();
                    if (!personEvals.Any()) continue;

                    sheet.Cell(row, 1).Value = p.FullName;
                    sheet.Cell(row, 2).Value = p.OrganizationName;

                    col = 3;
                    foreach (var period in monthlyPeriods)
                    {
                        var periodEvals = personEvals
                            .Where(e => e.CallDate!.Value.Year == period.Year && e.CallDate!.Value.Month == period.Month)
                            .ToList();
                        if (periodEvals.Any())
                        {
                            var avg = periodEvals.Average(e => (double)e.ScorePercentage!.Value);
                            sheet.Cell(row, col).Value = Math.Round(avg, 1);
                            if (avg >= 80)
                                sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGreen;
                            else if (avg >= 60)
                                sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightYellow;
                            else
                                sheet.Cell(row, col).Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightCoral;
                        }
                        else
                        {
                            sheet.Cell(row, col).Value = "-";
                        }
                        col++;
                    }

                    var overallAvg = personEvals.Average(e => (double)e.ScorePercentage!.Value);
                    sheet.Cell(row, col).Value = Math.Round(overallAvg, 1);
                    sheet.Cell(row, col).Style.Font.Bold = true;
                    sheet.Cell(row, col + 1).Value = personEvals.Count;

                    row++;
                }
            }

            sheet.Columns().AdjustToContents();
            ExcelHelper.ApplyLongTextColumnStyles(sheet);

            // ===== GENEL RAPOR SHEET =====
            // Düz veri: Proje, Temsilci, Departman, Kontrol Sorusu (GroupName), Periyot, Periyot (Ay), Ortalama Puan, Hata Sayısı
            var genelRaporQuery = _context.Answers
                .Include(a => a.Question)
                .Include(a => a.Evaluation)
                    .ThenInclude(e => e.Assignment)
                        .ThenInclude(asn => asn.Project)
                .Include(a => a.Evaluation)
                    .ThenInclude(e => e.AssignmentPeriod)
                .Include(a => a.Evaluation)
                    .ThenInclude(e => e.EvaluatedCustomerPersonnel)
                        .ThenInclude(cp => cp!.OrganizationAssignments)
                            .ThenInclude(oa => oa.CustomerOrganization)
                .Where(a => a.Evaluation.Assignment != null &&
                           a.Evaluation.Assignment.Project != null &&
                           a.Evaluation.Assignment.Project.CustomerId == customerId &&
                           a.Evaluation.StatusId == EvaluationStatuses.Ids.Completed &&
                           a.Question.GroupName != null &&
                           a.Question.WeightPoints > 0);

            if (startDate.HasValue)
            {
                var startUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
                genelRaporQuery = genelRaporQuery.Where(a =>
                    (a.Evaluation.AssignmentPeriod != null && a.Evaluation.AssignmentPeriod.EndDate >= startUtc) ||
                    (a.Evaluation.AssignmentPeriod == null && (a.Evaluation.CallDate ?? a.Evaluation.ControlDate ?? a.Evaluation.CreatedAt) >= startUtc));
            }
            if (endDate.HasValue)
            {
                var endUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
                genelRaporQuery = genelRaporQuery.Where(a =>
                    (a.Evaluation.AssignmentPeriod != null && a.Evaluation.AssignmentPeriod.StartDate <= endUtc) ||
                    (a.Evaluation.AssignmentPeriod == null && (a.Evaluation.CallDate ?? a.Evaluation.ControlDate ?? a.Evaluation.CreatedAt) <= endUtc));
            }

            var genelRaporAnswers = await genelRaporQuery
                .Select(a => new
                {
                    ProjectName = a.Evaluation.Assignment.Project.Name,
                    PersonnelId = a.Evaluation.EvaluatedCustomerPersonnelId,
                    PersonnelName = a.Evaluation.EvaluatedCustomerPersonnel != null
                        ? a.Evaluation.EvaluatedCustomerPersonnel.FirstName + " " + a.Evaluation.EvaluatedCustomerPersonnel.LastName
                        : "-",
                    OrgName = a.Evaluation.EvaluatedCustomerPersonnel != null
                        ? a.Evaluation.EvaluatedCustomerPersonnel.OrganizationAssignments
                            .Select(oa => oa.CustomerOrganization.Name)
                            .FirstOrDefault() ?? "-"
                        : "-",
                    GroupName = a.Question.GroupName!,
                    EarnedPoints = a.EarnedPoints ?? 0,
                    WeightPoints = a.Question.WeightPoints,
                    PeriodName = a.Evaluation.AssignmentPeriod != null ? a.Evaluation.AssignmentPeriod.Name : null,
                    PeriodStartDate = a.Evaluation.AssignmentPeriod != null ? a.Evaluation.AssignmentPeriod.StartDate : (DateTime?)null,
                    EvalDate = a.Evaluation.CallDate ?? a.Evaluation.ControlDate ?? a.Evaluation.CreatedAt
                })
                .ToListAsync();

            // Genel Rapor sheet'i her zaman ekle - PIVOT TABLO FORMATI
            var genelSheet = workbook.Worksheets.Add("Genel Rapor");

            if (genelRaporAnswers.Any())
            {
                // Önce GroupName + Personnel bazında grupla ve hesapla
                var pivotData = genelRaporAnswers
                    .GroupBy(a => new { a.GroupName, a.PersonnelId, a.PersonnelName })
                    .Select(g =>
                    {
                        var answers = g.ToList();
                        var sumWeight = answers.Sum(a => a.WeightPoints);
                        var sumEarned = answers.Sum(a => a.EarnedPoints);
                        return new
                        {
                            g.Key.GroupName,
                            g.Key.PersonnelId,
                            g.Key.PersonnelName,
                            AvgScore = sumWeight > 0 ? Math.Round(sumEarned / sumWeight * 100, 2) : 0,
                            ErrorCount = answers.Count(a => a.EarnedPoints < a.WeightPoints)
                        };
                    })
                    .ToList();

                // Unique değerleri al
                var groupNames = pivotData.Select(p => p.GroupName).Distinct().OrderBy(g => g).ToList();
                var personnelList = pivotData
                    .Select(p => new { p.PersonnelId, p.PersonnelName })
                    .Distinct()
                    .OrderBy(p => p.PersonnelName)
                    .ToList();

                // Row 1: Personel isimleri (her biri 2 sütun merge) + Toplam sütunları
                genelSheet.Cell(1, 1).Value = "Kontrol Sorusu";
                genelSheet.Cell(1, 1).Style.Font.Bold = true;
                int col = 2;
                foreach (var person in personnelList)
                {
                    genelSheet.Cell(1, col).Value = person.PersonnelName;
                    genelSheet.Cell(1, col).Style.Font.Bold = true;
                    genelSheet.Range(1, col, 1, col + 1).Merge();
                    genelSheet.Cell(1, col).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                    col += 2;
                }
                // Toplam sütun başlıkları
                int totalColStart = col;
                genelSheet.Cell(1, col).Value = "Ortalama Puan Toplamı";
                genelSheet.Cell(1, col).Style.Font.Bold = true;
                genelSheet.Cell(1, col + 1).Value = "Hata Sayısı Toplamı";
                genelSheet.Cell(1, col + 1).Style.Font.Bold = true;

                // Row 2: Alt başlıklar (Ortalama Puan, Hata Sayısı)
                genelSheet.Cell(2, 1).Value = "";
                col = 2;
                foreach (var _ in personnelList)
                {
                    genelSheet.Cell(2, col).Value = "Ortalama Puan";
                    genelSheet.Cell(2, col).Style.Font.Bold = true;
                    genelSheet.Cell(2, col + 1).Value = "Hata Sayısı";
                    genelSheet.Cell(2, col + 1).Style.Font.Bold = true;
                    col += 2;
                }
                genelSheet.Cell(2, totalColStart).Value = "";
                genelSheet.Cell(2, totalColStart + 1).Value = "";

                // Data rows: Her GroupName için bir satır
                int row = 3;
                foreach (var groupName in groupNames)
                {
                    genelSheet.Cell(row, 1).Value = groupName;
                    col = 2;
                    var rowScores = new List<decimal>();
                    var rowErrors = 0;
                    foreach (var person in personnelList)
                    {
                        var data = pivotData.FirstOrDefault(p => p.GroupName == groupName && p.PersonnelId == person.PersonnelId);
                        if (data != null)
                        {
                            genelSheet.Cell(row, col).Value = (double)data.AvgScore;
                            genelSheet.Cell(row, col + 1).Value = data.ErrorCount;
                            rowScores.Add(data.AvgScore);
                            rowErrors += data.ErrorCount;
                        }
                        else
                        {
                            genelSheet.Cell(row, col).Value = "-";
                            genelSheet.Cell(row, col + 1).Value = "-";
                        }
                        col += 2;
                    }
                    // Satır toplamları
                    if (rowScores.Any())
                    {
                        genelSheet.Cell(row, totalColStart).Value = (double)Math.Round(rowScores.Average(), 2);
                        genelSheet.Cell(row, totalColStart).Style.Font.Bold = true;
                    }
                    else
                    {
                        genelSheet.Cell(row, totalColStart).Value = "-";
                    }
                    genelSheet.Cell(row, totalColStart + 1).Value = rowErrors;
                    genelSheet.Cell(row, totalColStart + 1).Style.Font.Bold = true;
                    row++;
                }
            }
            else
            {
                genelSheet.Cell(1, 1).Value = "Veri bulunamadı";
            }

            genelSheet.Columns().AdjustToContents();
            ExcelHelper.ApplyLongTextColumnStyles(genelSheet);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);

            return File(
                stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"DonemBazliBasari_{DateTime.Now:yyyyMMdd}.xlsx"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting performance by period for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Excel oluşturulurken hata oluştu." });
        }
    }

    // ==================== EĞİTİM VİDEOLARI ====================

    /// <summary>
    /// Kullanıcının kendi eğitim videolarını listele
    /// </summary>
    [HttpGet("my-trainings")]
    public async Task<IActionResult> GetMyTrainings()
    {
        var personnelId = GetPersonnelId();

        // Admin kullanıcılar için boş liste dön (PersonnelId yok)
        if (!personnelId.HasValue)
            return Ok(new List<object>());

        var now = DateTime.UtcNow;

        var trainings = await _context.TrainingVideoParticipants
            .Include(p => p.Assignment)
                .ThenInclude(a => a.TrainingVideo)
            .Where(p => p.CustomerPersonnelId == personnelId.Value && !p.IsDeleted)
            .Where(p => p.Assignment.IsActive && !p.Assignment.IsDeleted)
            .OrderByDescending(p => p.Assignment.DueDate)
            .Select(p => new
            {
                participantId = p.Id,
                assignmentId = p.Assignment.Id,
                assignmentTitle = p.Assignment.Title,
                videoId = p.Assignment.TrainingVideo.Id,
                videoTitle = p.Assignment.TrainingVideo.Title,
                videoDescription = p.Assignment.TrainingVideo.Description,
                videoDurationSeconds = p.Assignment.TrainingVideo.DurationSeconds,
                startDate = p.Assignment.StartDate,
                dueDate = p.Assignment.DueDate,
                statusId = p.StatusId,
                statusName = p.StatusId == 1 ? "Bekliyor" : p.StatusId == 2 ? "İzleniyor" : "Tamamlandı",
                startedAt = p.StartedAt,
                completedAt = p.CompletedAt,
                watchedSeconds = p.WatchedSeconds,
                isCompleted = p.IsCompleted,
                isOverdue = p.Assignment.DueDate < now && !p.IsCompleted,
                daysRemaining = p.IsCompleted ? 0 : (int)Math.Max(0, (p.Assignment.DueDate - now).TotalDays),
                // Video izleme kuralları (Video'dan)
                minWatchPercentage = p.Assignment.TrainingVideo.MinWatchPercentage,
                allowSkipping = p.Assignment.TrainingVideo.AllowSkipping,
                maxPlaybackSpeed = p.Assignment.TrainingVideo.MaxPlaybackSpeed,
                // Atama izleme kuralları (Assignment'tan)
                allowSeeking = p.Assignment.AllowSeeking,
                allowSpeedChange = p.Assignment.AllowSpeedChange,
                // İzleme sayısı bilgileri
                watchCount = p.WatchCount,
                minWatchCount = p.Assignment.MinWatchCount,
                maxWatchCount = p.Assignment.MaxWatchCount,
                remainingWatches = p.Assignment.MaxWatchCount.HasValue
                    ? Math.Max(0, p.Assignment.MaxWatchCount.Value - p.WatchCount)
                    : (int?)null
            })
            .ToListAsync();

        return Ok(trainings);
    }

    /// <summary>
    /// Eğitim izleme ilerleme güncelle
    /// </summary>
    [HttpPost("my-trainings/{participantId}/progress")]
    public async Task<IActionResult> UpdateMyTrainingProgress(int participantId, [FromBody] UpdateWatchProgressDto dto)
    {
        var personnelId = GetPersonnelId();
        if (!personnelId.HasValue)
            return Unauthorized(new { message = "Personel bilgisi bulunamadı." });

        var participant = await _context.TrainingVideoParticipants
            .Include(p => p.Assignment)
                .ThenInclude(a => a.TrainingVideo)
            .FirstOrDefaultAsync(p => p.Id == participantId && p.CustomerPersonnelId == personnelId.Value && !p.IsDeleted);

        if (participant == null)
            return NotFound(new { message = "Eğitim kaydı bulunamadı." });

        var now = DateTime.UtcNow;

        // İlk izlemeye başlama
        if (participant.StatusId == 1 && dto.WatchedSeconds > 0)
        {
            participant.StatusId = 2;
            participant.StartedAt = now;
        }

        participant.WatchedSeconds = dto.WatchedSeconds;

        // Tamamlama kontrolü
        if (dto.IsCompleted || participant.WatchedSeconds >= participant.Assignment.TrainingVideo.DurationSeconds)
        {
            participant.IsCompleted = true;
            participant.StatusId = 3;
            participant.CompletedAt ??= now;
        }

        participant.UpdatedAt = now;
        await _context.SaveChangesAsync();

        return Ok(new {
            success = true,
            watchCount = participant.WatchCount,
            maxWatchCount = participant.Assignment.MaxWatchCount,
            remainingWatches = participant.Assignment.MaxWatchCount.HasValue
                ? Math.Max(0, participant.Assignment.MaxWatchCount.Value - participant.WatchCount)
                : (int?)null
        });
    }

    /// <summary>
    /// Video izleme oturumu başlat - izleme hakkını kullanır
    /// </summary>
    [HttpPost("my-trainings/{participantId}/start-session")]
    public async Task<IActionResult> StartWatchSession(int participantId)
    {
        var personnelId = GetPersonnelId();
        if (!personnelId.HasValue)
            return Unauthorized(new { message = "Personel bilgisi bulunamadı." });

        var participant = await _context.TrainingVideoParticipants
            .Include(p => p.Assignment)
            .FirstOrDefaultAsync(p => p.Id == participantId && p.CustomerPersonnelId == personnelId.Value && !p.IsDeleted);

        if (participant == null)
            return NotFound(new { message = "Eğitim kaydı bulunamadı." });

        var now = DateTime.UtcNow;

        // MaxWatchCount kontrolü
        var maxWatches = participant.Assignment.MaxWatchCount;
        if (maxWatches.HasValue && participant.WatchCount >= maxWatches.Value)
        {
            return BadRequest(new {
                success = false,
                message = "Maksimum izleme hakkınızı doldurdunuz.",
                watchCount = participant.WatchCount,
                maxWatchCount = maxWatches.Value
            });
        }

        // İzleme hakkını kullan
        participant.WatchCount++;

        // İlk izlemeye başlama
        if (participant.StatusId == 1)
        {
            participant.StatusId = 2;
            participant.StartedAt = now;
        }

        participant.UpdatedAt = now;
        await _context.SaveChangesAsync();

        return Ok(new {
            success = true,
            watchCount = participant.WatchCount,
            maxWatchCount = participant.Assignment.MaxWatchCount,
            remainingWatches = participant.Assignment.MaxWatchCount.HasValue
                ? Math.Max(0, participant.Assignment.MaxWatchCount.Value - participant.WatchCount)
                : (int?)null
        });
    }

    /// <summary>
    /// Yönetici/Süpervizör için personel eğitimlerini listele
    /// </summary>
    [HttpGet("staff-trainings")]
    public async Task<IActionResult> GetStaffTrainings()
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();

        // İzin verilen personel ID'lerini belirle
        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var now = DateTime.UtcNow;

        var query = _context.TrainingVideoParticipants
            .Include(p => p.CustomerPersonnel)
            .Include(p => p.Assignment)
                .ThenInclude(a => a.TrainingVideo)
            .Where(p => !p.IsDeleted && p.Assignment.IsActive && !p.Assignment.IsDeleted)
            .Where(p => p.CustomerPersonnel.CustomerId == customerId.Value);

        // CustomerManager tüm personeli görebilir
        // CustomerSupervisor sadece altındakileri görebilir
        if (allowedPersonnelIds != null)
        {
            query = query.Where(p => allowedPersonnelIds.Contains(p.CustomerPersonnelId));
        }

        var trainings = await query
            .OrderByDescending(p => p.Assignment.DueDate)
            .ThenBy(p => p.CustomerPersonnel.FirstName)
            .Select(p => new
            {
                participantId = p.Id,
                personnelId = p.CustomerPersonnelId,
                personnelName = p.CustomerPersonnel.FirstName + " " + p.CustomerPersonnel.LastName,
                personnelEmail = p.CustomerPersonnel.Email,
                assignmentId = p.Assignment.Id,
                assignmentTitle = p.Assignment.Title,
                videoId = p.Assignment.TrainingVideo.Id,
                videoTitle = p.Assignment.TrainingVideo.Title,
                videoDurationSeconds = p.Assignment.TrainingVideo.DurationSeconds,
                startDate = p.Assignment.StartDate,
                dueDate = p.Assignment.DueDate,
                statusId = p.StatusId,
                startedAt = p.StartedAt,
                completedAt = p.CompletedAt,
                watchedSeconds = p.WatchedSeconds,
                isCompleted = p.IsCompleted,
                isOverdue = p.Assignment.DueDate < now && !p.IsCompleted,
                daysRemaining = p.IsCompleted ? 0 : (int)Math.Max(0, (p.Assignment.DueDate - now).TotalDays)
            })
            .ToListAsync();

        return Ok(new { trainings });
    }

    /// <summary>
    /// İzleme ilerleme DTO
    /// </summary>
    public class UpdateWatchProgressDto
    {
        public int WatchedSeconds { get; set; }
        public bool IsCompleted { get; set; }
    }

    // ==================== SURVEY RESULTS (Reports) ====================

    /// <summary>
    /// Müşterinin Online Survey projelerini listeler
    /// </summary>
    [HttpGet("reports/survey-projects")]
    public async Task<IActionResult> GetSurveyProjects()
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var projects = await _context.Projects
            .Where(p => p.CustomerId == customerId.Value &&
                   p.ProjectTypeId == Core.Enums.ProjectTypes.Ids.OnlineSurvey &&
                   !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var result = new List<object>();

        foreach (var project in projects)
        {
            // Davetiye sayısı
            var internalInvitations = await _context.SurveyInvitations
                .Where(si => si.ProjectId == project.Id &&
                       (si.StatusId == Core.Enums.SurveyInvitationStatuses.Ids.Sent || si.StatusId == Core.Enums.SurveyInvitationStatuses.Ids.Pending))
                .CountAsync();

            var externalInvitations = await _context.SurveyExternalInvitations
                .Where(si => si.ProjectId == project.Id &&
                       (si.StatusId == Core.Enums.SurveyInvitationStatuses.Ids.Sent || si.StatusId == Core.Enums.SurveyInvitationStatuses.Ids.Pending))
                .CountAsync();

            var invitationCount = internalInvitations + externalInvitations;

            // Tamamlanan anket sayısı
            var completedCount = await _context.Evaluations
                .Where(e => e.Assignment.ProjectId == project.Id && e.StatusId == Core.Enums.EvaluationStatuses.Ids.Completed)
                .CountAsync();

            // Ortalama puan
            var avgScore = await _context.Evaluations
                .Where(e => e.Assignment.ProjectId == project.Id &&
                       e.StatusId == Core.Enums.EvaluationStatuses.Ids.Completed &&
                       e.ScorePercentage.HasValue)
                .Select(e => e.ScorePercentage)
                .AverageAsync() ?? 0;

            // Son yanıt tarihi
            var lastResponse = await _context.Evaluations
                .Where(e => e.Assignment.ProjectId == project.Id && e.StatusId == Core.Enums.EvaluationStatuses.Ids.Completed)
                .OrderByDescending(e => e.CompletedAt)
                .Select(e => e.CompletedAt)
                .FirstOrDefaultAsync();

            result.Add(new
            {
                projectId = project.Id,
                projectName = project.Name,
                projectCode = project.Code,
                totalInvitations = invitationCount,
                totalResponses = completedCount,
                responseRate = invitationCount > 0 ? Math.Round((decimal)completedCount / invitationCount * 100, 1) : 0,
                averageScore = completedCount > 0 ? Math.Round(avgScore, 1) : (decimal?)null,
                lastResponseAt = lastResponse,
                isActive = project.IsActive
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Son anket yanıtlarını listeler
    /// </summary>
    [HttpGet("reports/survey-responses/recent")]
    public async Task<IActionResult> GetRecentSurveyResponses(
        [FromQuery] int count = 20,
        [FromQuery] int? projectId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Where(e => e.Assignment.Project.CustomerId == customerId.Value &&
                   e.Assignment.Project.ProjectTypeId == Core.Enums.ProjectTypes.Ids.OnlineSurvey &&
                   e.StatusId == Core.Enums.EvaluationStatuses.Ids.Completed &&
                   !e.Assignment.Project.IsDeleted)
            .AsQueryable();

        if (projectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == projectId.Value);

        if (startDate.HasValue)
        {
            var startDateUtc = DateTime.SpecifyKind(startDate.Value.Date, DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt >= startDateUtc);
        }

        if (endDate.HasValue)
        {
            var endDateUtc = DateTime.SpecifyKind(endDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Utc);
            query = query.Where(e => e.CompletedAt <= endDateUtc);
        }

        var evaluations = await query
            .OrderByDescending(e => e.CompletedAt)
            .Take(count)
            .ToListAsync();

        // External invitations for evaluations without CustomerPersonnel
        var evaluationIds = evaluations.Where(e => e.EvaluatedCustomerPersonnelId == null).Select(e => e.Id).ToList();
        var externalInvitations = new Dictionary<int, (string? FirstName, string? LastName, string? Email)>();
        if (evaluationIds.Any())
        {
            var extList = await _context.SurveyExternalInvitations
                .Where(sei => sei.EvaluationId != null && evaluationIds.Contains(sei.EvaluationId.Value))
                .Select(sei => new { EvalId = sei.EvaluationId!.Value, sei.FirstName, sei.LastName, sei.Email })
                .ToListAsync();
            foreach (var item in extList)
                externalInvitations[item.EvalId] = (item.FirstName, item.LastName, item.Email);
        }

        var responses = evaluations.Select(e =>
        {
            string? respondentName = null;
            string? respondentEmail = null;

            if (e.EvaluatedCustomerPersonnel != null)
            {
                respondentName = $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}".Trim();
                respondentEmail = e.EvaluatedCustomerPersonnel.Email;
            }
            else if (externalInvitations.TryGetValue(e.Id, out var ext))
            {
                respondentName = $"{ext.FirstName} {ext.LastName}".Trim();
                respondentEmail = ext.Email;
            }

            return new
            {
                evaluationId = e.Id,
                projectId = e.Assignment.ProjectId,
                projectName = e.Assignment.Project.Name,
                respondentName = string.IsNullOrWhiteSpace(respondentName) ? null : respondentName,
                respondentEmail,
                score = e.ScorePercentage,
                completedAt = e.CompletedAt
            };
        }).ToList();

        return Ok(responses);
    }

    /// <summary>
    /// Proje detayı - grup puanları ve son katılımcılar
    /// </summary>
    [HttpGet("reports/survey-projects/{projectId}/detail")]
    public async Task<IActionResult> GetSurveyProjectDetail(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var project = await _context.Projects
            .Include(p => p.Organization)
            .Include(p => p.Checklist)
            .FirstOrDefaultAsync(p => p.Id == projectId &&
                   p.CustomerId == customerId.Value &&
                   p.ProjectTypeId == Core.Enums.ProjectTypes.Ids.OnlineSurvey &&
                   !p.IsDeleted);

        if (project == null)
            return NotFound(new { message = "Proje bulunamadı." });

        // Sorular
        var questions = await _context.Questions
            .Where(q => q.ChecklistId == project.ChecklistId && !q.IsDeleted)
            .ToListAsync();

        // Değerlendirmeler
        var evaluations = await _context.Evaluations
            .Include(e => e.Answers)
            .Include(e => e.EvaluatedCustomerPersonnel)
                .ThenInclude(p => p!.OrganizationAssignments)
                    .ThenInclude(oa => oa.CustomerOrganization)
            .Where(e => e.Assignment.ProjectId == projectId && e.StatusId == Core.Enums.EvaluationStatuses.Ids.Completed)
            .OrderByDescending(e => e.CompletedAt)
            .ToListAsync();

        // Davetiye sayısı
        var invitationCount = await _context.SurveyInvitations
            .Where(si => si.ProjectId == projectId && si.StatusId == Core.Enums.SurveyInvitationStatuses.Ids.Sent)
            .CountAsync();

        // Grup bazlı puan hesaplaması
        var groupScores = new List<object>();
        var groups = questions.GroupBy(q => q.GroupName ?? "Genel");

        foreach (var group in groups)
        {
            var groupQuestionIds = group.Select(q => q.Id).ToList();
            var groupAnswers = evaluations
                .SelectMany(e => e.Answers.Where(a => groupQuestionIds.Contains(a.QuestionId) && a.AnswerNumeric.HasValue))
                .ToList();

            decimal? avgScore = null;
            if (groupAnswers.Any())
            {
                var totalScore = 0m;
                var totalMaxScore = 0m;

                foreach (var question in group.Where(q => q.ShowScoreInput))
                {
                    var questionAnswers = groupAnswers.Where(a => a.QuestionId == question.Id).ToList();
                    if (questionAnswers.Any())
                    {
                        totalScore += questionAnswers.Sum(a => a.AnswerNumeric ?? 0);
                        totalMaxScore += questionAnswers.Count * question.MaxPoints;
                    }
                }

                avgScore = totalMaxScore > 0 ? Math.Round(totalScore / totalMaxScore * 100, 1) : null;
            }

            groupScores.Add(new
            {
                groupName = group.Key ?? "Genel",
                questionCount = group.Count(),
                totalResponses = evaluations.Count,
                averageScore = avgScore
            });
        }

        // Son 10 katılımcı - External invitation'ları da al
        var top10 = evaluations.Take(10).ToList();
        var extEvalIds = top10.Where(e => e.EvaluatedCustomerPersonnelId == null).Select(e => e.Id).ToList();
        var extInvs = new Dictionary<int, (string? FirstName, string? LastName, string? Email)>();
        if (extEvalIds.Any())
        {
            var extList = await _context.SurveyExternalInvitations
                .Where(sei => sei.EvaluationId != null && extEvalIds.Contains(sei.EvaluationId.Value))
                .Select(sei => new { EvalId = sei.EvaluationId!.Value, sei.FirstName, sei.LastName, sei.Email })
                .ToListAsync();
            foreach (var item in extList)
                extInvs[item.EvalId] = (item.FirstName, item.LastName, item.Email);
        }

        var recentRespondents = top10.Select(e =>
        {
            string? fullName = null;
            string? email = null;
            string? orgName = null;

            if (e.EvaluatedCustomerPersonnel != null)
            {
                fullName = $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}".Trim();
                email = e.EvaluatedCustomerPersonnel.Email;
                orgName = e.EvaluatedCustomerPersonnel.OrganizationAssignments.FirstOrDefault()?.CustomerOrganization?.Name;
            }
            else if (extInvs.TryGetValue(e.Id, out var ext))
            {
                fullName = $"{ext.FirstName} {ext.LastName}".Trim();
                email = ext.Email;
            }

            return new
            {
                personnelId = e.EvaluatedCustomerPersonnelId ?? 0,
                evaluationId = e.Id,
                fullName = string.IsNullOrWhiteSpace(fullName) ? null : fullName,
                email,
                organizationName = orgName,
                score = e.ScorePercentage,
                completedAt = e.CompletedAt
            };
        }).ToList();

        return Ok(new
        {
            projectId = project.Id,
            projectName = project.Name,
            organizationName = project.Organization?.Name,
            totalInvitations = invitationCount > 0 ? invitationCount : evaluations.Count,
            totalResponses = evaluations.Count,
            responseRate = invitationCount > 0 ? Math.Round((decimal)evaluations.Count / invitationCount * 100, 1) : 100,
            averageScore = evaluations.Any(e => e.ScorePercentage.HasValue)
                ? Math.Round((decimal)evaluations.Where(e => e.ScorePercentage.HasValue).Average(e => e.ScorePercentage!.Value), 1)
                : (decimal?)null,
            totalQuestions = questions.Count,
            groupScores = groupScores.OrderBy(g => ((dynamic)g).groupName).ToList(),
            recentRespondents
        });
    }

    /// <summary>
    /// Soru bazlı puan dağılımı
    /// </summary>
    [HttpGet("reports/survey-question-distribution")]
    public async Task<IActionResult> GetSurveyQuestionScoreDistribution(
        [FromQuery] int? projectId = null)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        // Proje zorunlu
        if (!projectId.HasValue)
            return Ok(new { questions = new List<object>(), totalResponses = 0, overallAverageScore = 0 });

        // Proje müşteriye ait mi kontrol et
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId.Value &&
                   p.CustomerId == customerId.Value &&
                   p.ProjectTypeId == Core.Enums.ProjectTypes.Ids.OnlineSurvey &&
                   !p.IsDeleted);

        if (project == null)
            return Ok(new { questions = new List<object>(), totalResponses = 0, overallAverageScore = 0 });

        // Değerlendirme ID'leri
        var evaluationIds = await _context.Evaluations
            .Where(e => e.Assignment.ProjectId == projectId.Value &&
                   e.StatusId == Core.Enums.EvaluationStatuses.Ids.Completed)
            .Select(e => e.Id)
            .ToListAsync();

        if (!evaluationIds.Any())
            return Ok(new { questions = new List<object>(), totalResponses = 0, overallAverageScore = 0 });

        // Cevapları ve soruları getir (ReportService gibi)
        var answers = await _context.Answers
            .Include(a => a.Question)
            .Where(a => evaluationIds.Contains(a.EvaluationId) && !a.Question.IsDeleted)
            .ToListAsync();

        // Soru bazlı gruplama - EarnedPoints kullan
        var questionStats = answers
            .GroupBy(a => new
            {
                a.QuestionId,
                a.Question.Text,
                a.Question.GroupName,
                a.Question.Order,
                a.Question.WeightPoints
            })
            .Select(g =>
            {
                var avgRaw = g.Where(a => a.EarnedPoints.HasValue).Any()
                    ? (decimal?)Math.Round(g.Where(a => a.EarnedPoints.HasValue).Average(a => a.EarnedPoints!.Value), 2)
                    : null;

                var avgPercent = g.Where(a => a.EarnedPoints.HasValue).Any() && g.Key.WeightPoints > 0
                    ? (decimal?)Math.Round(g.Where(a => a.EarnedPoints.HasValue).Average(a => a.EarnedPoints!.Value) / g.Key.WeightPoints * 100, 1)
                    : null;

                return new
                {
                    questionId = g.Key.QuestionId,
                    questionText = g.Key.Text,
                    groupName = g.Key.GroupName ?? "Genel",
                    order = g.Key.Order,
                    maxPoints = (int)g.Key.WeightPoints,
                    responseCount = g.Count(),
                    averageRawScore = avgRaw,
                    averageScore = avgPercent
                };
            })
            .OrderBy(q => q.groupName)
            .ThenBy(q => q.order)
            .ToList();

        // Genel ortalama hesapla
        var overallAverage = questionStats.Where(q => q.averageScore.HasValue).Any()
            ? Math.Round(questionStats.Where(q => q.averageScore.HasValue).Average(q => q.averageScore!.Value), 1)
            : 0m;

        return Ok(new
        {
            questions = questionStats,
            totalResponses = evaluationIds.Count,
            overallAverageScore = overallAverage
        });
    }

    /// <summary>
    /// Proje için soru bazlı puan detayı
    /// </summary>
    [HttpGet("reports/survey-projects/{projectId}/score-detail")]
    public async Task<IActionResult> GetSurveyQuestionScoreDetail(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId &&
                   p.CustomerId == customerId.Value &&
                   p.ProjectTypeId == Core.Enums.ProjectTypes.Ids.OnlineSurvey &&
                   !p.IsDeleted);

        if (project == null)
            return NotFound(new { message = "Proje bulunamadı." });

        // Sorular
        var questions = await _context.Questions
            .Include(q => q.SubCriteria.Where(sc => !sc.IsDeleted))
            .Where(q => q.ChecklistId == project.ChecklistId && !q.IsDeleted)
            .OrderBy(q => q.Order)
            .ToListAsync();

        // Değerlendirmeler ve cevapları
        var evaluations = await _context.Evaluations
            .Include(e => e.Answers)
                .ThenInclude(a => a.SubCriteriaSelections)
            .Where(e => e.Assignment.ProjectId == projectId && e.StatusId == Core.Enums.EvaluationStatuses.Ids.Completed)
            .ToListAsync();

        var questionDetails = questions.Select(q =>
        {
            var answers = evaluations.SelectMany(e => e.Answers.Where(a => a.QuestionId == q.Id)).ToList();

            // Puan dağılımı
            var scoreDistribution = new Dictionary<int, int>();
            if (q.ShowScoreInput)
            {
                for (int i = 0; i <= q.MaxPoints; i++)
                    scoreDistribution[i] = 0;

                foreach (var answer in answers.Where(a => a.AnswerNumeric.HasValue))
                {
                    var score = (int)(answer.AnswerNumeric ?? 0);
                    if (scoreDistribution.ContainsKey(score))
                        scoreDistribution[score]++;
                }
            }

            // Alt kriter seçim dağılımı
            var subCriteriaStats = q.SubCriteria.Select(sc =>
            {
                var selectedCount = answers.Count(a => a.SubCriteriaSelections.Any(s => s.SubCriteriaId == sc.Id));
                return new
                {
                    subCriteriaId = sc.Id,
                    description = sc.Description,
                    selectedCount,
                    percentage = answers.Any() ? Math.Round((decimal)selectedCount / answers.Count * 100, 1) : 0
                };
            }).ToList();

            return new
            {
                questionId = q.Id,
                questionText = q.Text,
                groupName = q.GroupName ?? "Genel",
                maxPoints = q.MaxPoints,
                showScoreInput = q.ShowScoreInput,
                responseCount = answers.Count,
                averageScore = q.ShowScoreInput && answers.Any(a => a.AnswerNumeric.HasValue)
                    ? (decimal)Math.Round(answers.Where(a => a.AnswerNumeric.HasValue).Average(a => a.AnswerNumeric!.Value), 2)
                    : (decimal?)null,
                scoreDistribution = scoreDistribution.OrderBy(d => d.Key).Select(d => new { score = d.Key, count = d.Value }).ToList(),
                subCriteriaStats
            };
        }).ToList();

        return Ok(new
        {
            projectId = project.Id,
            projectName = project.Name,
            totalResponses = evaluations.Count,
            questions = questionDetails
        });
    }

    /// <summary>
    /// Grup puanları raporu Excel export
    /// </summary>
    [HttpGet("reports/survey-results/{projectId}/export/group-scores")]
    public async Task<IActionResult> ExportSurveyGroupScores(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        // Proje kontrolü
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId &&
                   p.CustomerId == customerId.Value &&
                   p.ProjectTypeId == Core.Enums.ProjectTypes.Ids.OnlineSurvey &&
                   !p.IsDeleted);

        if (project == null)
            return NotFound(new { message = "Proje bulunamadı." });

        var result = await _reportService.ExportSurveyGroupScoresToExcelAsync(projectId);
        if (result == null)
            return NotFound(new { message = "Rapor oluşturulamadı." });

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Soru istatistikleri raporu Excel export
    /// </summary>
    [HttpGet("reports/survey-results/{projectId}/export/question-stats")]
    public async Task<IActionResult> ExportSurveyQuestionStats(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId &&
                   p.CustomerId == customerId.Value &&
                   p.ProjectTypeId == Core.Enums.ProjectTypes.Ids.OnlineSurvey &&
                   !p.IsDeleted);

        if (project == null)
            return NotFound(new { message = "Proje bulunamadı." });

        var result = await _reportService.ExportSurveyQuestionStatsToExcelAsync(projectId);
        if (result == null)
            return NotFound(new { message = "Rapor oluşturulamadı." });

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Detay raporu Excel export (yorumsuz)
    /// </summary>
    [HttpGet("reports/survey-results/{projectId}/export/detail")]
    public async Task<IActionResult> ExportSurveyDetailReport(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId &&
                   p.CustomerId == customerId.Value &&
                   p.ProjectTypeId == Core.Enums.ProjectTypes.Ids.OnlineSurvey &&
                   !p.IsDeleted);

        if (project == null)
            return NotFound(new { message = "Proje bulunamadı." });

        var result = await _reportService.ExportSurveyDetailReportToExcelAsync(projectId, includeComments: false);
        if (result == null)
            return NotFound(new { message = "Rapor oluşturulamadı." });

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Detay raporu Excel export (yorumlu)
    /// </summary>
    [HttpGet("reports/survey-results/{projectId}/export/full-detail")]
    public async Task<IActionResult> ExportSurveyFullDetailReport(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId &&
                   p.CustomerId == customerId.Value &&
                   p.ProjectTypeId == Core.Enums.ProjectTypes.Ids.OnlineSurvey &&
                   !p.IsDeleted);

        if (project == null)
            return NotFound(new { message = "Proje bulunamadı." });

        var result = await _reportService.ExportSurveyDetailReportToExcelAsync(projectId, includeComments: true);
        if (result == null)
            return NotFound(new { message = "Rapor oluşturulamadı." });

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Soru puan detay raporu Excel export
    /// </summary>
    [HttpGet("reports/survey-results/{projectId}/export/score-detail")]
    public async Task<IActionResult> ExportSurveyScoreDetail(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId &&
                   p.CustomerId == customerId.Value &&
                   p.ProjectTypeId == Core.Enums.ProjectTypes.Ids.OnlineSurvey &&
                   !p.IsDeleted);

        if (project == null)
            return NotFound(new { message = "Proje bulunamadı." });

        var result = await _reportService.ExportSurveyQuestionScoreDetailAsync(projectId);
        if (result == null)
            return NotFound(new { message = "Rapor oluşturulamadı." });

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Taslak değerlendirmeyi siler
    /// - Kullanıcı kendi taslağını silebilir
    /// - CustomerManager tüm taslakları silebilir
    /// - Sadece Draft durumundakiler silinebilir
    /// </summary>
    [HttpDelete("evaluations/{id:int}")]
    public async Task<IActionResult> DeleteDraft(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();
        var isManager = role == "CustomerManager" || User.IsInRole("Admin");

        // Değerlendirmeyi bul
        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted);

        if (evaluation == null)
        {
            return NotFound(new { message = "Değerlendirme bulunamadı." });
        }

        // Müşteri kontrolü
        if (evaluation.Assignment?.Project?.CustomerId != customerId)
        {
            return Forbid();
        }

        // Sadece Draft durumundakiler silinebilir
        if (evaluation.StatusId != Core.Enums.EvaluationStatuses.Ids.Draft)
        {
            return BadRequest(new { message = "Sadece taslak durumundaki değerlendirmeler silinebilir." });
        }

        // Yetki kontrolü: Manager değilse kendi taslağı olmalı
        if (!isManager && personnelId.HasValue)
        {
            if (evaluation.EvaluatorCustomerPersonnelId != personnelId.Value)
            {
                return Forbid();
            }
        }

        // Soft delete
        evaluation.IsDeleted = true;
        evaluation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Taslak değerlendirme silindi (CustomerPortal): EvaluationId={EvaluationId}, PersonnelId={PersonnelId}, IsManager={IsManager}",
            id, personnelId, isManager);

        return Ok(new { message = "Taslak başarıyla silindi." });
    }

    /// <summary>
    /// İç dinleme taslağını siler (Sadece Manager)
    /// </summary>
    [HttpDelete("evaluations/internal/{id:int}")]
    public async Task<IActionResult> DeleteInternalDraft(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var role = GetPersonnelRole();
        var isManager = role == "CustomerManager" || User.IsInRole("Admin");

        // Sadece Manager silebilir
        if (!isManager)
        {
            return Forbid();
        }

        // Değerlendirmeyi bul
        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .FirstOrDefaultAsync(e => e.Id == id && !e.IsDeleted && e.EvaluatorCustomerPersonnelId != null);

        if (evaluation == null)
        {
            return NotFound(new { message = "Değerlendirme bulunamadı." });
        }

        // Müşteri kontrolü
        if (evaluation.Assignment?.Project?.CustomerId != customerId)
        {
            return Forbid();
        }

        // Sadece Draft durumundakiler silinebilir
        if (evaluation.StatusId != EvaluationStatuses.Ids.Draft)
        {
            return BadRequest(new { message = "Sadece taslak durumundaki değerlendirmeler silinebilir." });
        }

        // Soft delete
        evaluation.IsDeleted = true;
        evaluation.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("İç dinleme taslağı silindi (CustomerPortal): EvaluationId={EvaluationId}, Role={Role}",
            id, role);

        return Ok(new { message = "Taslak başarıyla silindi." });
    }

    // ==================== ENNEAGRAM RESULTS ====================

    /// <summary>
    /// Müşterinin Enneagram projelerini listeler
    /// </summary>
    [HttpGet("reports/enneagram-projects")]
    public async Task<IActionResult> GetEnneagramProjects()
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        // Enneagram checklistlerini bul
        var enneagramChecklistIds = await _context.Checklists
            .Where(c => c.ChecklistTypeId == Core.Enums.ChecklistTypes.Ids.Enneagram && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        var projects = await _context.Projects
            .Where(p => p.CustomerId == customerId.Value &&
                   enneagramChecklistIds.Contains(p.ChecklistId) &&
                   !p.IsDeleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var result = new List<object>();

        foreach (var project in projects)
        {
            // Tamamlanan değerlendirme sayısı
            var completedCount = await _context.Evaluations
                .Where(e => e.Assignment.ProjectId == project.Id && e.StatusId == Core.Enums.EvaluationStatuses.Ids.Completed)
                .CountAsync();

            result.Add(new
            {
                projectId = project.Id,
                projectName = project.Name,
                totalResponses = completedCount,
                isActive = project.IsActive
            });
        }

        return Ok(result);
    }

    /// <summary>
    /// Enneagram sonuçlarını listeler
    /// </summary>
    [HttpGet("reports/enneagram-results")]
    public async Task<IActionResult> GetEnneagramResults(
        [FromQuery] int? projectId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        // Enneagram checklistlerini bul
        var enneagramChecklistIds = await _context.Checklists
            .Where(c => c.ChecklistTypeId == Core.Enums.ChecklistTypes.Ids.Enneagram && !c.IsDeleted)
            .Select(c => c.Id)
            .ToListAsync();

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Where(e => e.Assignment.Project.CustomerId == customerId.Value &&
                   enneagramChecklistIds.Contains(e.Assignment.Project.ChecklistId) &&
                   e.StatusId == Core.Enums.EvaluationStatuses.Ids.Completed &&
                   !e.Assignment.Project.IsDeleted)
            .AsQueryable();

        if (projectId.HasValue)
            query = query.Where(e => e.Assignment.ProjectId == projectId.Value);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var term = searchTerm.Trim().ToLower();
            query = query.Where(e =>
                (e.EvaluatedCustomerPersonnel != null &&
                    (e.EvaluatedCustomerPersonnel.FirstName.ToLower().Contains(term) ||
                     e.EvaluatedCustomerPersonnel.LastName.ToLower().Contains(term) ||
                     (e.EvaluatedCustomerPersonnel.Email != null && e.EvaluatedCustomerPersonnel.Email.ToLower().Contains(term)))));
        }

        var totalCount = await query.CountAsync();

        var evaluations = await query
            .OrderByDescending(e => e.CompletedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        // Enneagram sorularını al (Grup adı = kişilik tipi)
        var checklistIds = evaluations.Select(e => e.Assignment.Project.ChecklistId).Distinct().ToList();
        var questions = await _context.Questions
            .Where(q => checklistIds.Contains(q.ChecklistId) && !q.IsDeleted)
            .ToListAsync();

        // External invitations
        var evaluationIds = evaluations.Where(e => e.EvaluatedCustomerPersonnelId == null).Select(e => e.Id).ToList();
        var externalInvitations = new Dictionary<int, (string? FirstName, string? LastName, string? Email)>();
        if (evaluationIds.Any())
        {
            var extList = await _context.SurveyExternalInvitations
                .Where(sei => sei.EvaluationId != null && evaluationIds.Contains(sei.EvaluationId.Value))
                .Select(sei => new { EvalId = sei.EvaluationId!.Value, sei.FirstName, sei.LastName, sei.Email })
                .ToListAsync();
            foreach (var item in extList)
                externalInvitations[item.EvalId] = (item.FirstName, item.LastName, item.Email);
        }

        // Cevapları al
        var allEvaluationIds = evaluations.Select(e => e.Id).ToList();
        var answers = await _context.Answers
            .Where(a => allEvaluationIds.Contains(a.EvaluationId))
            .ToListAsync();

        var results = new List<object>();

        foreach (var e in evaluations)
        {
            string? respondentName = null;
            string? respondentEmail = null;

            if (e.EvaluatedCustomerPersonnel != null)
            {
                respondentName = $"{e.EvaluatedCustomerPersonnel.FirstName} {e.EvaluatedCustomerPersonnel.LastName}".Trim();
                respondentEmail = e.EvaluatedCustomerPersonnel.Email;
            }
            else if (externalInvitations.TryGetValue(e.Id, out var ext))
            {
                respondentName = $"{ext.FirstName} {ext.LastName}".Trim();
                respondentEmail = ext.Email;
            }

            // Kişilik tipi skorlarını hesapla
            var evalAnswers = answers.Where(a => a.EvaluationId == e.Id).ToList();
            var evalQuestions = questions.Where(q => q.ChecklistId == e.Assignment.Project.ChecklistId).ToList();

            var personalityScores = evalQuestions
                .GroupBy(q => q.GroupName ?? "Bilinmeyen")
                .Select(g => new
                {
                    personalityType = g.Key,
                    totalPoints = evalAnswers.Where(a => g.Select(q => q.Id).Contains(a.QuestionId)).Sum(a => a.GivenPoints ?? 0),
                    maxPoints = g.Sum(q => (int)q.WeightPoints)
                })
                .OrderByDescending(x => x.totalPoints)
                .ToList();

            var dominant = personalityScores.FirstOrDefault();

            results.Add(new
            {
                evaluationId = e.Id,
                projectId = e.Assignment.ProjectId,
                projectName = e.Assignment.Project.Name,
                respondentName = string.IsNullOrWhiteSpace(respondentName) ? null : respondentName,
                respondentEmail,
                dominantType = dominant?.personalityType,
                dominantPercentage = dominant != null && dominant.maxPoints > 0
                    ? Math.Round((decimal)dominant.totalPoints / dominant.maxPoints * 100, 1)
                    : (decimal?)null,
                totalScore = e.ScorePercentage,
                completedAt = e.CompletedAt
            });
        }

        // Summary
        var allResults = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment.Project.CustomerId == customerId.Value &&
                   enneagramChecklistIds.Contains(e.Assignment.Project.ChecklistId) &&
                   e.StatusId == Core.Enums.EvaluationStatuses.Ids.Completed &&
                   !e.Assignment.Project.IsDeleted)
            .ToListAsync();

        var projectCount = allResults.Select(e => e.Assignment.ProjectId).Distinct().Count();

        return Ok(new
        {
            results,
            totalCount,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize),
            summary = new
            {
                totalResponses = allResults.Count,
                projectCount,
                dominantType = "-",
                averageCompletionRate = (decimal?)null
            }
        });
    }

    /// <summary>
    /// Tek bir Enneagram sonuç detayı
    /// </summary>
    [HttpGet("reports/enneagram-results/{evaluationId}")]
    public async Task<IActionResult> GetEnneagramResultDetail(int evaluationId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var evaluation = await _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.Answers)
            .FirstOrDefaultAsync(e => e.Id == evaluationId &&
                   e.Assignment.Project.CustomerId == customerId.Value);

        if (evaluation == null)
            return NotFound(new { message = "Sonuç bulunamadı." });

        // Respondent info
        string? respondentName = null;
        string? respondentEmail = null;

        if (evaluation.EvaluatedCustomerPersonnel != null)
        {
            respondentName = $"{evaluation.EvaluatedCustomerPersonnel.FirstName} {evaluation.EvaluatedCustomerPersonnel.LastName}".Trim();
            respondentEmail = evaluation.EvaluatedCustomerPersonnel.Email;
        }
        else
        {
            var ext = await _context.SurveyExternalInvitations
                .Where(sei => sei.EvaluationId == evaluationId)
                .Select(sei => new { sei.FirstName, sei.LastName, sei.Email })
                .FirstOrDefaultAsync();
            if (ext != null)
            {
                respondentName = $"{ext.FirstName} {ext.LastName}".Trim();
                respondentEmail = ext.Email;
            }
        }

        // Sorular ve gruplar
        var questions = await _context.Questions
            .Where(q => q.ChecklistId == evaluation.Assignment.Project.ChecklistId && !q.IsDeleted)
            .ToListAsync();

        // Kişilik tipi skorları
        var scores = questions
            .GroupBy(q => q.GroupName ?? "Bilinmeyen")
            .Select(g => new
            {
                personalityType = g.Key,
                totalPoints = evaluation.Answers.Where(a => g.Select(q => q.Id).Contains(a.QuestionId)).Sum(a => a.GivenPoints ?? 0),
                maxPoints = (int)g.Sum(q => q.WeightPoints),
                percentage = g.Sum(q => q.WeightPoints) > 0
                    ? Math.Round((decimal)evaluation.Answers.Where(a => g.Select(q => q.Id).Contains(a.QuestionId)).Sum(a => a.GivenPoints ?? 0) / g.Sum(q => q.WeightPoints) * 100, 1)
                    : 0
            })
            .OrderByDescending(x => x.percentage)
            .ToList();

        var dominant = scores.FirstOrDefault();

        return Ok(new
        {
            evaluationId = evaluation.Id,
            projectId = evaluation.Assignment.ProjectId,
            projectName = evaluation.Assignment.Project.Name,
            respondentName = string.IsNullOrWhiteSpace(respondentName) ? null : respondentName,
            respondentEmail,
            dominantType = dominant?.personalityType,
            completedAt = evaluation.CompletedAt,
            scores
        });
    }

    /// <summary>
    /// Enneagram kişilik tipi dağılımı (proje bazlı)
    /// </summary>
    [HttpGet("reports/enneagram-distribution/{projectId}")]
    public async Task<IActionResult> GetEnneagramDistribution(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId &&
                   p.CustomerId == customerId.Value &&
                   !p.IsDeleted);

        if (project == null)
            return NotFound(new { message = "Proje bulunamadı." });

        // Tamamlanan değerlendirmeler
        var evaluations = await _context.Evaluations
            .Include(e => e.Answers)
            .Where(e => e.Assignment.ProjectId == projectId &&
                   e.StatusId == Core.Enums.EvaluationStatuses.Ids.Completed)
            .ToListAsync();

        if (!evaluations.Any())
        {
            return Ok(new
            {
                projectId,
                projectName = project.Name,
                totalResponses = 0,
                distribution = new List<object>()
            });
        }

        // Sorular ve gruplar
        var questions = await _context.Questions
            .Where(q => q.ChecklistId == project.ChecklistId && !q.IsDeleted)
            .ToListAsync();

        // Kişilik tiplerine göre toplam puanlar
        var personalityGroups = questions
            .GroupBy(q => q.GroupName ?? "Bilinmeyen")
            .Select(g => new
            {
                personalityType = g.Key,
                questionIds = g.Select(q => q.Id).ToList(),
                maxPointsPerResponse = (int)g.Sum(q => q.WeightPoints)
            })
            .ToList();

        var distribution = personalityGroups.Select(pg =>
        {
            var totalPoints = evaluations
                .SelectMany(e => e.Answers)
                .Where(a => pg.questionIds.Contains(a.QuestionId))
                .Sum(a => a.GivenPoints ?? 0);

            var maxPoints = pg.maxPointsPerResponse * evaluations.Count;
            var avgPercentage = maxPoints > 0 ? Math.Round((decimal)totalPoints / maxPoints * 100, 1) : 0;

            return new
            {
                pg.personalityType,
                totalPoints,
                maxPoints = pg.maxPointsPerResponse,
                responseCount = evaluations.Count,
                averagePercentage = avgPercentage
            };
        })
        .OrderByDescending(x => x.averagePercentage)
        .ToList();

        return Ok(new
        {
            projectId,
            projectName = project.Name,
            totalResponses = evaluations.Count,
            distribution
        });
    }

    // ==================== PUAN EŞİKLERİ (Score Thresholds) ====================

    /// <summary>
    /// Müşteriye özel puan eşiklerini getirir (tüm proje tipleri için)
    /// </summary>
    [HttpGet("score-thresholds")]
    public async Task<IActionResult> GetScoreThresholds()
    {
        try
        {
            var customerId = GetCustomerId();
            if (!customerId.HasValue)
                return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

            var result = await _customerScoreThresholdService.GetAllAsync(customerId.Value);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error getting score thresholds");
            return StatusCode(500, new { message = "Puan eşikleri yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Müşteriye özel puan eşiklerini toplu kaydeder
    /// </summary>
    [HttpPost("score-thresholds/bulk")]
    public async Task<IActionResult> BulkSaveScoreThresholds([FromBody] Core.DTOs.CustomerScoreThreshold.BulkSaveCustomerScoreThresholdDto dto)
    {
        try
        {
            var customerId = GetCustomerId();
            if (!customerId.HasValue)
                return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var result = await _customerScoreThresholdService.BulkSaveAsync(customerId.Value, dto, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error saving score thresholds");
            return StatusCode(500, new { message = "Puan eşikleri kaydedilirken hata oluştu." });
        }
    }

    // ===== ŞUBE KARNESİ =====

    [HttpGet("reports/dealer-list")]
    public async Task<IActionResult> GetDealerList()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var dealers = await _reportService.GetDealerListAsync(customerId.Value);
            return Ok(dealers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading dealer list for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Şube listesi yüklenirken hata oluştu." });
        }
    }

    [HttpGet("reports/dealer-report-card/{dealerId}")]
    public async Task<IActionResult> GetDealerReportCard(
        int dealerId,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Güvenlik kontrolü: Dealer'ın bu müşteriye ait olup olmadığını doğrula
            var dealer = await _context.CustomerDealers.FindAsync(dealerId);
            if (dealer == null || dealer.CustomerId != customerId.Value)
                return NotFound(new { message = "Şube bulunamadı." });

            var filter = new DealerReportCardFilterDto
            {
                DealerId = dealerId,
                ProjectIds = projectIds,
                DateRanges = dateRanges
            };

            var result = await _reportService.GetDealerReportCardAsync(filter);

            if (result == null)
                return NotFound(new { message = "Şube bulunamadı." });

            // EvaluatorName alanlarını temizle (müşteri görmemeli)
            foreach (var evaluation in result.RecentEvaluations)
            {
                evaluation.EvaluatorName = null;
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading dealer report card for customer {CustomerId}, dealer {DealerId}", customerId, dealerId);
            return StatusCode(500, new { message = "Şube karnesi yüklenirken hata oluştu.", error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("reports/dealer-report-card/{dealerId}/export")]
    public async Task<IActionResult> ExportDealerReportCard(
        int dealerId,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var dealer = await _context.CustomerDealers.FindAsync(dealerId);
            if (dealer == null || dealer.CustomerId != customerId.Value)
                return NotFound(new { message = "Şube bulunamadı." });

            var filter = new DealerReportCardFilterDto
            {
                DealerId = dealerId,
                ProjectIds = projectIds,
                DateRanges = dateRanges
            };

            var result = await _reportService.ExportDealerReportCardToExcelAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting dealer report card for customer {CustomerId}, dealer {DealerId}", customerId, dealerId);
            return StatusCode(500, new { message = "Şube karnesi export edilirken hata oluştu." });
        }
    }

    [HttpGet("reports/dealer-report-card/{dealerId}/export-word")]
    public async Task<IActionResult> ExportDealerReportCardToWord(
        int dealerId,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var dealer = await _context.CustomerDealers
                .FirstOrDefaultAsync(d => d.Id == dealerId && d.CustomerId == customerId);

            if (dealer == null)
                return NotFound(new { message = "Şube bulunamadı." });

            var filter = new DealerReportCardFilterDto
            {
                DealerId = dealerId,
                ProjectIds = projectIds,
                DateRanges = dateRanges
            };

            var result = await _reportService.ExportDealerReportCardToWordAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting dealer report card to Word for customer {CustomerId}, dealer {DealerId}", customerId, dealerId);
            return StatusCode(500, new { message = "Şube karnesi Word export edilirken hata oluştu." });
        }
    }

}
