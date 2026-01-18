using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,QualitySpecialist")]
public class ReportsApiController : BaseApiController
{
    private readonly IReportService _reportService;
    private readonly ICustomerService _customerService;
    private readonly ICustomerOrganizationService _organizationService;
    private readonly IProjectService _projectService;
    private readonly IUserService _userService;
    private readonly ILogger<ReportsApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public ReportsApiController(
        IReportService reportService,
        ICustomerService customerService,
        ICustomerOrganizationService organizationService,
        IProjectService projectService,
        IUserService userService,
        ILogger<ReportsApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _reportService = reportService;
        _customerService = customerService;
        _organizationService = organizationService;
        _projectService = projectService;
        _userService = userService;
        _logger = logger;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Filtreler için lookup verileri (müşteri, organizasyon, proje, değerlendirici)
    /// </summary>
    [HttpGet("lookups")]
    public async Task<IActionResult> GetLookups([FromQuery] List<int>? customerIds = null)
    {
        try
        {
            var customers = await _customerService.GetActiveAsync();
            var projects = await _projectService.GetAllAsync(includeInactive: true); // Listenings filtresi için tüm projeler
            var evaluators = await _userService.GetByRoleIdAsync(UserRoles.Ids.QualitySpecialist);

            // DateRangeTypes with localized names
            var dateRanges = new List<object>();
            foreach (var d in DateRangeTypes.All)
            {
                dateRanges.Add(new
                {
                    d.Id,
                    d.SystemName,
                    Name = await _localizationService.GetResourceAsync(d.NameResourceKey)
                });
            }

            // Evaluation source types (simplified: our evaluations vs customer internal)
            var evaluationSources = new List<object>
            {
                new { id = "ours", name = await _localizationService.GetResourceAsync("Listenings.EvaluationSource.Ours") },
                new { id = "internal", name = await _localizationService.GetResourceAsync("Listenings.EvaluationSource.Internal") }
            };

            return Ok(new
            {
                customers = customers.Select(c => new { c.Id, c.CompanyName }),
                projects = projects.Select(p => new { p.Id, p.Name, p.Code, p.CustomerId }),
                evaluators = evaluators.Select(e => new { e.Id, Name = $"{e.FirstName} {e.LastName}" }),
                dateRanges,
                evaluationSources
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading report lookups");
            return StatusCode(500, CreateErrorResponse("Lookup verileri yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Organizasyon listesi (müşteriye göre)
    /// </summary>
    [HttpGet("organizations/{customerId}")]
    public async Task<IActionResult> GetOrganizationsByCustomer(int customerId)
    {
        try
        {
            var organizations = await _organizationService.GetByCustomerIdAsync(customerId);
            return Ok(organizations.Select(o => new { o.Id, o.Name }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading organizations for customer {CustomerId}", customerId);
            return StatusCode(500, CreateErrorResponse("Organizasyonlar yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Değerlendirme listesi (sayfalı)
    /// </summary>
    [HttpPost("evaluations")]
    public async Task<IActionResult> GetEvaluations([FromBody] ReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.GetEvaluationsAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluations report");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.LoadError"), ex));
        }
    }

    /// <summary>
    /// Değerlendirme sayısı (ayrı endpoint - hızlı ilk yükleme için)
    /// </summary>
    [HttpPost("evaluations/count")]
    public async Task<IActionResult> GetEvaluationsCount([FromBody] ReportFilterDto filter)
    {
        try
        {
            var count = await _reportService.GetEvaluationsCountAsync(filter);
            return Ok(new { totalCount = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evaluations count");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.LoadError"), ex));
        }
    }

    /// <summary>
    /// Değerlendirme detayı
    /// </summary>
    [HttpGet("evaluations/{evaluationId}")]
    public async Task<IActionResult> GetEvaluationDetail(int evaluationId)
    {
        try
        {
            var result = await _reportService.GetEvaluationDetailAsync(evaluationId);
            if (result == null)
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.NotFound")));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluation detail {EvaluationId}", evaluationId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.EvaluationDetailLoadError"), ex));
        }
    }

    /// <summary>
    /// Değerlendirme detayı Excel export
    /// </summary>
    [HttpGet("evaluations/{evaluationId}/export")]
    public async Task<IActionResult> ExportEvaluationDetail(int evaluationId)
    {
        try
        {
            var result = await _reportService.ExportEvaluationDetailToExcelAsync(evaluationId);
            if (result == null)
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Evaluation.NotFound")));

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting evaluation detail {EvaluationId}", evaluationId);
            return StatusCode(500, CreateErrorResponse("Değerlendirme detayı export edilirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Özet rapor
    /// </summary>
    [HttpPost("summary")]
    public async Task<IActionResult> GetSummaryReport([FromBody] ReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.GetSummaryReportAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading summary report");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.SummaryLoadError"), ex));
        }
    }

    /// <summary>
    /// Excel export - Değerlendirmeler
    /// </summary>
    [HttpPost("export/excel")]
    public async Task<IActionResult> ExportToExcel([FromBody] ReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.ExportEvaluationsToExcelAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting evaluations to Excel");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.ExcelExportError"), ex));
        }
    }

    /// <summary>
    /// Detaylı Excel export - Soru cevaplarıyla
    /// </summary>
    [HttpPost("export/excel/detailed")]
    public async Task<IActionResult> ExportDetailedToExcel([FromBody] ReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.ExportDetailedEvaluationsToExcelAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting detailed evaluations to Excel");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.DetailedExcelExportError"), ex));
        }
    }

    /// <summary>
    /// Çağrı Denetleme Raporu - Excel export
    /// </summary>
    [HttpPost("export/call-audit")]
    public async Task<IActionResult> ExportCallAuditReport([FromBody] ReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.ExportCallAuditReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting call audit report to Excel");
            return StatusCode(500, CreateErrorResponse("Çağrı denetleme raporu oluşturulurken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Soru Grubu Ortalama Raporu - Excel export
    /// </summary>
    [HttpPost("export/question-group-average")]
    public async Task<IActionResult> ExportQuestionGroupAverageReport([FromBody] ReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.ExportQuestionGroupAverageReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting question group average report to Excel");
            return StatusCode(500, CreateErrorResponse("Soru grubu ortalama raporu oluşturulurken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Müşteri Değerlendirme Raporu - Excel export
    /// </summary>
    [HttpPost("export/customer-evaluation")]
    public async Task<IActionResult> ExportCustomerEvaluationReport([FromBody] ReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.ExportCustomerEvaluationReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting customer evaluation report to Excel");
            return StatusCode(500, CreateErrorResponse("Müşteri değerlendirme raporu oluşturulurken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Proje Performans Raporu - Excel export
    /// </summary>
    [HttpPost("export/project-performance")]
    public async Task<IActionResult> ExportProjectPerformanceReport([FromBody] ReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.ExportProjectPerformanceReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting project performance report to Excel");
            return StatusCode(500, CreateErrorResponse("Proje performans raporu oluşturulurken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// MT Raporu - Excel export (4 sheet: Başarı, Gelişim Alanı, Süreç Analizi, Endeks Başarı)
    /// </summary>
    [HttpPost("export/mt-report")]
    public async Task<IActionResult> ExportMTReport([FromBody] ReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.ExportMTReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting MT report to Excel");
            return StatusCode(500, CreateErrorResponse("MT raporu oluşturulurken hata oluştu.", ex));
        }
    }

    // ===== CEZALI KL RAPORU =====

    /// <summary>
    /// Cezalı KL Raporu
    /// </summary>
    [HttpGet("penalties")]
    public async Task<IActionResult> GetPenaltiesReport(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? customerIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] List<int>? evaluatorIds,
        [FromQuery] List<string>? penaltyTypes,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var filter = new PenaltyFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = customerIds,
                OrganizationIds = organizationIds,
                ChecklistIds = checklistIds,
                EvaluatorIds = evaluatorIds,
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
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading penalties report");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.PenaltiesLoadError"), ex));
        }
    }

    /// <summary>
    /// Cezalı KL Raporu Excel Export
    /// </summary>
    [HttpGet("penalties/export")]
    public async Task<IActionResult> ExportPenaltiesToExcel(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? customerIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] List<int>? evaluatorIds,
        [FromQuery] List<string>? penaltyTypes,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var filter = new PenaltyFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = customerIds,
                OrganizationIds = organizationIds,
                ChecklistIds = checklistIds,
                EvaluatorIds = evaluatorIds,
                PenaltyTypes = penaltyTypes,
                Page = 1,
                PageSize = int.MaxValue // Export için pagination yok
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.ExportPenaltiesToExcelAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting penalties to Excel");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.PenaltiesExportError"), ex));
        }
    }

    // ===== TEMSİLCİ KARNESİ (Video 4) =====

    /// <summary>
    /// Değerlendirmesi olan müşteri listesini getirir (karne için seçim)
    /// </summary>
    [HttpGet("report-card/customers")]
    public async Task<IActionResult> GetReportCardCustomers()
    {
        try
        {
            var customers = await _reportService.GetCustomersWithEvaluationsAsync();
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customers for report card");
            return StatusCode(500, CreateErrorResponse("Müşteri listesi yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Değerlendirmesi olan organizasyon listesini getirir (müşteriye göre filtrelenir)
    /// </summary>
    [HttpGet("report-card/organizations")]
    public async Task<IActionResult> GetReportCardOrganizations([FromQuery] List<int>? customerIds = null)
    {
        try
        {
            var customerId = customerIds?.FirstOrDefault();
            var organizations = await _reportService.GetOrganizationsWithEvaluationsAsync(customerId);
            return Ok(organizations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading organizations for report card");
            return StatusCode(500, CreateErrorResponse("Organizasyon listesi yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Değerlendirilen personel listesini getirir (karne için seçim)
    /// </summary>
    [HttpGet("personnel-list")]
    public async Task<IActionResult> GetPersonnelList(
        [FromQuery] List<int>? customerIds = null,
        [FromQuery] List<int>? organizationIds = null)
    {
        try
        {
            var customerId = customerIds?.FirstOrDefault();
            var organizationId = organizationIds?.FirstOrDefault();
            var personnel = await _reportService.GetEvaluatedPersonnelListAsync(customerId, organizationId);
            return Ok(personnel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading personnel list for report card");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.LoadListError"), ex));
        }
    }

    /// <summary>
    /// Personelin değerlendirildiği projeleri getirir (karne filtresi için)
    /// </summary>
    [HttpGet("personnel-projects/{personnelId:int}")]
    public async Task<IActionResult> GetPersonnelProjects(int personnelId)
    {
        try
        {
            var projects = await _reportService.GetPersonnelProjectsAsync(personnelId);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading projects for personnel {PersonnelId}", personnelId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.LoadError"), ex));
        }
    }

    /// <summary>
    /// Temsilci Karnesi raporunu getirir
    /// </summary>
    [HttpGet("personnel-report-card/{personnelId:int}")]
    public async Task<IActionResult> GetPersonnelReportCard(
        int personnelId,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds
            };

            // DateRanges pattern: startDate/endDate -> DateRanges
            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.GetPersonnelReportCardAsync(filter);
            if (result == null)
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.NotFound")));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading personnel report card for {PersonnelId}", personnelId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.ReportCardLoadError"), ex));
        }
    }

    /// <summary>
    /// Temsilci Karnesi PDF/Excel export
    /// </summary>
    [HttpGet("personnel-report-card/{personnelId:int}/export")]
    public async Task<IActionResult> ExportPersonnelReportCard(
        int personnelId,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds
            };

            // DateRanges pattern: startDate/endDate -> DateRanges
            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.ExportPersonnelReportCardToPdfAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting personnel report card for {PersonnelId}", personnelId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.ReportCardExportError"), ex));
        }
    }

    // ===== ÖNERİLER RAPORU (Video 5-6) =====

    /// <summary>
    /// Öneriler Raporu - Tüm değerlendirmelerdeki önerilerin listesi
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestionsReport(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? customerIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] List<int>? evaluatorIds,
        [FromQuery] List<int>? personnelIds,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? searchText,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var filter = new SuggestionsFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = customerIds,
                OrganizationIds = organizationIds,
                ChecklistIds = checklistIds,
                EvaluatorIds = evaluatorIds,
                PersonnelIds = personnelIds,
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
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading suggestions report");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.SuggestionsLoadError"), ex));
        }
    }

    /// <summary>
    /// En çok öneri yazılan sorular
    /// </summary>
    [HttpGet("suggestions/top-questions")]
    public async Task<IActionResult> GetTopSuggestedQuestions(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? customerIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int top = 10)
    {
        try
        {
            var filter = new SuggestionsFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = customerIds,
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
            _logger.LogError(ex, "Error loading top suggested questions");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.TopSuggestionsLoadError"), ex));
        }
    }

    // ===== ANKET SONUÇLARI RAPORU =====

    /// <summary>
    /// Anket Sonuçları Raporu - Online Survey projeleri için
    /// </summary>
    [HttpGet("survey-results/{projectId}")]
    public async Task<IActionResult> GetSurveyResults(
        int projectId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var result = await _reportService.GetSurveyResultsAsync(projectId, startDate, endDate);
            if (result == null)
                return NotFound(CreateErrorResponse("Proje bulunamadı veya online anket projesi değil."));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading survey results for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse("Anket sonuçları yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Online Anket Projeleri Listesi (Dashboard için)
    /// </summary>
    [HttpGet("survey-projects")]
    public async Task<IActionResult> GetSurveyProjects()
    {
        try
        {
            var result = await _reportService.GetSurveyProjectsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading survey projects");
            return StatusCode(500, CreateErrorResponse("Anket projeleri yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Son Anket Yanıtları (Dashboard sol panel için)
    /// </summary>
    [HttpGet("survey-responses/recent")]
    public async Task<IActionResult> GetRecentSurveyResponses(
        [FromQuery] int count = 10,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var projectId = projectIds?.FirstOrDefault();
            var result = await _reportService.GetRecentSurveyResponsesAsync(count, projectId, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading recent survey responses");
            return StatusCode(500, CreateErrorResponse("Son anket yanıtları yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Anket Proje Detayı (Modal için - Grup bazlı puanlarla)
    /// </summary>
    [HttpGet("survey-projects/{projectId}/detail")]
    public async Task<IActionResult> GetSurveyProjectDetail(int projectId)
    {
        try
        {
            var result = await _reportService.GetSurveyProjectDetailAsync(projectId);
            if (result == null)
                return NotFound(CreateErrorResponse("Proje bulunamadı veya online anket projesi değil."));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading survey project detail for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse("Proje detayı yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Genel Soru Puan Dağılımı (tüm online anket projeleri için)
    /// </summary>
    [HttpGet("survey-question-distribution")]
    public async Task<IActionResult> GetSurveyQuestionScoreDistribution(
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var projectId = projectIds?.FirstOrDefault();
            var result = await _reportService.GetSurveyQuestionScoreDistributionAsync(projectId, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading survey question score distribution");
            return StatusCode(500, CreateErrorResponse("Soru puan dağılımı yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Anket Sonuçları Excel Export
    /// </summary>
    [HttpGet("survey-results/{projectId}/export")]
    public async Task<IActionResult> ExportSurveyResults(
        int projectId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var result = await _reportService.ExportSurveyResultsToExcelAsync(projectId, startDate, endDate);
            if (result == null)
                return NotFound(CreateErrorResponse("Proje bulunamadı veya online anket projesi değil."));

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting survey results for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse("Anket sonuçları export edilirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Grup Bazlı Puan Raporu Excel Export
    /// </summary>
    [HttpGet("survey-results/{projectId}/export/group-scores")]
    public async Task<IActionResult> ExportSurveyGroupScores(int projectId)
    {
        try
        {
            var result = await _reportService.ExportSurveyGroupScoresToExcelAsync(projectId);
            if (result == null)
                return NotFound(CreateErrorResponse("Proje bulunamadı veya online anket projesi değil."));

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting group scores for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse("Grup puanları export edilirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Soru İstatistik Raporu Excel Export (alt seçenek kaç kere seçilmiş)
    /// </summary>
    [HttpGet("survey-results/{projectId}/export/question-stats")]
    public async Task<IActionResult> ExportSurveyQuestionStats(int projectId)
    {
        try
        {
            var result = await _reportService.ExportSurveyQuestionStatsToExcelAsync(projectId);
            if (result == null)
                return NotFound(CreateErrorResponse("Proje bulunamadı veya online anket projesi değil."));

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting question stats for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse("Soru istatistikleri export edilirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Detay Raporu Excel Export (puan + seçenekler)
    /// </summary>
    [HttpGet("survey-results/{projectId}/export/detail")]
    public async Task<IActionResult> ExportSurveyDetailReport(int projectId)
    {
        try
        {
            var result = await _reportService.ExportSurveyDetailReportToExcelAsync(projectId, includeComments: false);
            if (result == null)
                return NotFound(CreateErrorResponse("Proje bulunamadı veya online anket projesi değil."));

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting detail report for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse("Detay raporu export edilirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Tam Detay Raporu Excel Export (puan + seçenekler + yorumlar)
    /// </summary>
    [HttpGet("survey-results/{projectId}/export/full-detail")]
    public async Task<IActionResult> ExportSurveyFullDetailReport(int projectId)
    {
        try
        {
            var result = await _reportService.ExportSurveyDetailReportToExcelAsync(projectId, includeComments: true);
            if (result == null)
                return NotFound(CreateErrorResponse("Proje bulunamadı veya online anket projesi değil."));

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting full detail report for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse("Tam detay raporu export edilirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Online Anket - Soru Puan Detayı ve Cevap Dağılımları (Puan Detayı Modalı için)
    /// </summary>
    [HttpGet("survey-projects/{projectId}/score-detail")]
    public async Task<IActionResult> GetSurveyQuestionScoreDetail(int projectId)
    {
        try
        {
            var result = await _reportService.GetSurveyQuestionScoreDetailAsync(projectId);
            if (result == null)
                return NotFound(CreateErrorResponse("Proje bulunamadı."));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading score detail for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse("Puan detayı yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Online Anket - Soru Puan Detayı Excel Export
    /// </summary>
    [HttpGet("survey-results/{projectId}/export/score-detail")]
    public async Task<IActionResult> ExportSurveyQuestionScoreDetail(int projectId)
    {
        try
        {
            var result = await _reportService.ExportSurveyQuestionScoreDetailAsync(projectId);
            if (result.FileContent.Length == 0)
                return NotFound(CreateErrorResponse("Proje bulunamadı."));

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting score detail for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse("Puan detayı export edilirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Öneriler Raporu Excel Export
    /// </summary>
    [HttpGet("suggestions/export")]
    public async Task<IActionResult> ExportSuggestionsToExcel(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? customerIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] List<int>? evaluatorIds,
        [FromQuery] List<int>? personnelIds,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? searchText)
    {
        try
        {
            var filter = new SuggestionsFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = customerIds,
                OrganizationIds = organizationIds,
                ChecklistIds = checklistIds,
                EvaluatorIds = evaluatorIds,
                PersonnelIds = personnelIds,
                SearchText = searchText
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.ExportSuggestionsToExcelAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting suggestions to Excel");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.SuggestionsExportError"), ex));
        }
    }

    /// <summary>
    /// Performans Takibi raporu - Dinleyici performansları ve firma kota durumları (Admin Only)
    /// </summary>
    [HttpGet("performance-tracking")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetPerformanceTracking(
        [FromQuery] List<int>? customerIds = null,
        [FromQuery] List<int>? evaluatorIds = null,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var result = await _reportService.GetPerformanceTrackingAsync(
                customerIds, evaluatorIds, projectIds, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance tracking report");
            return StatusCode(500, CreateErrorResponse("Performans takibi raporu yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Personel Soru Bazlı Performans Raporu - Tablo görünümü
    /// </summary>
    [HttpGet("personnel-question-performance")]
    public async Task<IActionResult> GetPersonnelQuestionPerformance(
        [FromQuery] List<int>? customerIds = null,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] List<int>? personnelIds = null,
        [FromQuery] List<int>? periodIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var filter = new PersonnelQuestionPerformanceFilterDto
            {
                CustomerIds = customerIds,
                ProjectIds = projectIds,
                OrganizationIds = organizationIds,
                PersonnelIds = personnelIds,
                PeriodIds = periodIds
            };

            // Tarih aralığı varsa DateRanges'a ekle
            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.GetPersonnelQuestionPerformanceAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personnel question performance report");
            return StatusCode(500, CreateErrorResponse("Personel soru performans raporu yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Personel Soru Bazlı Performans Raporu Excel Export
    /// </summary>
    [HttpGet("personnel-question-performance/export")]
    public async Task<IActionResult> ExportPersonnelQuestionPerformanceReport(
        [FromQuery] List<int>? customerIds = null,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] List<int>? personnelIds = null,
        [FromQuery] List<int>? periodIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var filter = new PersonnelQuestionPerformanceFilterDto
            {
                CustomerIds = customerIds,
                ProjectIds = projectIds,
                OrganizationIds = organizationIds,
                PersonnelIds = personnelIds,
                PeriodIds = periodIds
            };

            // Tarih aralığı varsa DateRanges'a ekle
            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.ExportPersonnelQuestionPerformanceReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting personnel question performance report");
            return StatusCode(500, CreateErrorResponse("Personel soru performans raporu export edilirken hata oluştu.", ex));
        }
    }
}
