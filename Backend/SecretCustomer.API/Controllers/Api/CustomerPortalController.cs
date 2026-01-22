using ClosedXML.Excel;
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
    private readonly IEvaluationService _evaluationService;

    public CustomerPortalApiController(
        ApplicationDbContext context,
        ILogger<CustomerPortalApiController> logger,
        ILocalizationService localizationService,
        IReportService reportService,
        IEvaluationService evaluationService)
    {
        _context = context;
        _logger = logger;
        _localizationService = localizationService;
        _reportService = reportService;
        _evaluationService = evaluationService;
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

            // Hiçbir organizasyona atanmamış → sadece kendisi
            if (!myOrgIds.Any())
                return new List<int> { personnelId.Value };

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
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.ScorePercentage.HasValue);

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
    /// Puan dağılımı kategorisindeki değerlendirmeler (tıklanan renge göre)
    /// </summary>
    [HttpGet("dashboard/score-distribution/evaluations")]
    public async Task<IActionResult> GetScoreDistributionEvaluations(
        [FromQuery] string category,
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
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.ScorePercentage.HasValue);

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

        // Kategori filtresi
        switch (category?.ToLower())
        {
            case "excellent":
                evaluationsQuery = evaluationsQuery.Where(e => e.ScorePercentage >= 90);
                break;
            case "good":
                evaluationsQuery = evaluationsQuery.Where(e => e.ScorePercentage >= 80 && e.ScorePercentage < 90);
                break;
            case "average":
                evaluationsQuery = evaluationsQuery.Where(e => e.ScorePercentage >= 60 && e.ScorePercentage < 80);
                break;
            case "poor":
                evaluationsQuery = evaluationsQuery.Where(e => e.ScorePercentage < 60);
                break;
            default:
                return BadRequest(new { message = "Geçersiz kategori. Geçerli değerler: excellent, good, average, poor" });
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
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.ScorePercentage.HasValue);

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

        // Kategori filtresi
        var categoryLabel = "";
        switch (category?.ToLower())
        {
            case "excellent":
                evaluationsQuery = evaluationsQuery.Where(e => e.ScorePercentage >= 90);
                categoryLabel = "Mükemmel (90+)";
                break;
            case "good":
                evaluationsQuery = evaluationsQuery.Where(e => e.ScorePercentage >= 80 && e.ScorePercentage < 90);
                categoryLabel = "İyi (80-89)";
                break;
            case "average":
                evaluationsQuery = evaluationsQuery.Where(e => e.ScorePercentage >= 60 && e.ScorePercentage < 80);
                categoryLabel = "Orta (60-79)";
                break;
            case "poor":
                evaluationsQuery = evaluationsQuery.Where(e => e.ScorePercentage < 60);
                categoryLabel = "Düşük (<60)";
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

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"PuanDagilimi_{categoryLabel.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
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
        var query = _context.Projects
            .Where(p => p.CustomerId == customerId && p.IsActive && !p.IsDeleted);

        // Proje tipi filtresi
        if (projectTypeId.HasValue)
            query = query.Where(p => p.ProjectTypeId == projectTypeId.Value);

        var projects = await query
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

        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();

        var query = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Checklist)
            .Where(e => e.Assignment != null && e.Assignment.Project != null &&
                        e.Assignment.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed); // PRENSIP: Taslaklar rapora dahil edilmez

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
    /// Puan aralığına göre değerlendirmeler (detay modal için)
    /// </summary>
    [HttpGet("reports/score-range")]
    public async Task<IActionResult> GetEvaluationsByScoreRange(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] decimal minScore,
        [FromQuery] decimal maxScore)
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
            .Include(e => e.EvaluatedPersonnel)
            .Where(e => e.Assignment != null && e.Assignment.Project != null && e.Assignment.Project.CustomerId == customerId
                && e.CreatedAt >= start
                && e.CreatedAt <= end
                && e.StatusId == EvaluationStatuses.Ids.Completed
                && e.ScorePercentage.HasValue
                && e.ScorePercentage >= minScore
                && e.ScorePercentage < maxScore)
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

        // Organizasyonları al
        var organizationsQuery = _context.CustomerOrganizations
            .Where(o => o.CustomerId == customerId && o.IsActive && !o.IsDeleted);

        if (organizationIds?.Any() == true)
        {
            organizationsQuery = organizationsQuery.Where(o => organizationIds.Contains(o.Id));
        }

        var organizations = await organizationsQuery
            .OrderBy(o => o.Name)
            .Select(o => new { o.Id, o.Name })
            .ToListAsync();

        // Değerlendirmeleri al
        var evaluationsQuery = _context.Evaluations
            .Include(e => e.Assignment)
                .ThenInclude(a => a.Project)
            .Where(e => e.Assignment != null && e.Assignment.Project != null &&
                        e.Assignment.Project.CustomerId == customerId &&
                        e.StatusId == EvaluationStatuses.Ids.Completed &&
                        e.EvaluatedOrganizationId.HasValue &&
                        e.CreatedAt >= start &&
                        e.CreatedAt <= end);

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

        // Genel trend
        var overallTrend = new List<object>();
        foreach (var (rangeStart, rangeEnd) in dateRanges)
        {
            var periodEvals = evaluations.Where(e => e.CreatedAt >= rangeStart && e.CreatedAt <= rangeEnd).ToList();
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
                               e.CreatedAt >= rangeStart && e.CreatedAt <= rangeEnd &&
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

        // Süpervizör olan personelleri bul (CustomerPersonnelOrganization'da SupervisorId olarak geçenler)
        var supervisorIdsQuery = _context.CustomerPersonnelOrganizations
            .Where(cpo => cpo.SupervisorId.HasValue &&
                          cpo.CustomerOrganization.CustomerId == customerId);

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

        // Her süpervizörün takımındaki personel ID'lerini al
        var supervisorTeams = await _context.CustomerPersonnelOrganizations
            .Where(cpo => cpo.SupervisorId.HasValue && supervisorIds.Contains(cpo.SupervisorId.Value))
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

        var supervisors = await supervisorsQuery
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
                    .Count(cpo => cpo.SupervisorId == cp.Id)
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
            .Where(e => e.Assignment.Project.CustomerId == customerId &&
                       e.EvaluatorCustomerPersonnelId != null); // Draftlar da görünsün

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

            var detail = await _evaluationService.GetByIdAsync(evaluationId);
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
            var filter = new PenaltyFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value }, // Otomatik müşteri filtresi
                OrganizationIds = organizationIds,
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
            var filter = new SuggestionsFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value }, // Otomatik müşteri filtresi
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
            // Geriye uyumluluk: tekil organizationId parametresi için array'in ilk elemanını kullan
            var organizationId = organizationIds?.FirstOrDefault();
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

            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds,
                DateRanges = dateRanges
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
}
