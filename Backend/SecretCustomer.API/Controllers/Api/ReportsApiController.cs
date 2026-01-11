using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,TeamLeader")]
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
    public async Task<IActionResult> GetLookups([FromQuery] int? customerId = null)
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

    // ===== CEZALI KL RAPORU =====

    /// <summary>
    /// Cezalı KL Raporu
    /// </summary>
    [HttpGet("penalties")]
    public async Task<IActionResult> GetPenaltiesReport(
        [FromQuery] int? projectId,
        [FromQuery] string? penaltyType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var filter = new PenaltyFilterDto
            {
                ProjectId = projectId,
                PenaltyType = penaltyType,
                StartDate = startDate,
                EndDate = endDate
            };
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
        [FromQuery] int? projectId,
        [FromQuery] string? penaltyType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var filter = new PenaltyFilterDto
            {
                ProjectId = projectId,
                PenaltyType = penaltyType,
                StartDate = startDate,
                EndDate = endDate
            };
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
    /// Değerlendirilen personel listesini getirir (karne için seçim)
    /// </summary>
    [HttpGet("personnel-list")]
    public async Task<IActionResult> GetPersonnelList()
    {
        try
        {
            var personnel = await _reportService.GetEvaluatedPersonnelListAsync();
            return Ok(personnel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading personnel list for report card");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.LoadListError"), ex));
        }
    }

    /// <summary>
    /// Temsilci Karnesi raporunu getirir
    /// </summary>
    [HttpGet("personnel-report-card/{personnelId:int}")]
    public async Task<IActionResult> GetPersonnelReportCard(
        int personnelId,
        [FromQuery] int? projectId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectId = projectId,
                StartDate = startDate,
                EndDate = endDate
            };

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
        [FromQuery] int? projectId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectId = projectId,
                StartDate = startDate,
                EndDate = endDate
            };

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
        [FromQuery] int? projectId,
        [FromQuery] int? checklistId,
        [FromQuery] int? evaluatorId,
        [FromQuery] int? personnelId,
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
                ProjectId = projectId,
                ChecklistId = checklistId,
                EvaluatorId = evaluatorId,
                PersonnelId = personnelId,
                StartDate = startDate,
                EndDate = endDate,
                SearchText = searchText,
                Page = page,
                PageSize = pageSize
            };
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
        [FromQuery] int? projectId,
        [FromQuery] int? checklistId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int top = 10)
    {
        try
        {
            var filter = new SuggestionsFilterDto
            {
                ProjectId = projectId,
                ChecklistId = checklistId,
                StartDate = startDate,
                EndDate = endDate
            };
            var result = await _reportService.GetTopSuggestedQuestionsAsync(filter, top);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading top suggested questions");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.TopSuggestionsLoadError"), ex));
        }
    }

    /// <summary>
    /// Öneriler Raporu Excel Export
    /// </summary>
    [HttpGet("suggestions/export")]
    public async Task<IActionResult> ExportSuggestionsToExcel(
        [FromQuery] int? projectId,
        [FromQuery] int? checklistId,
        [FromQuery] int? evaluatorId,
        [FromQuery] int? personnelId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? searchText)
    {
        try
        {
            var filter = new SuggestionsFilterDto
            {
                ProjectId = projectId,
                ChecklistId = checklistId,
                EvaluatorId = evaluatorId,
                PersonnelId = personnelId,
                StartDate = startDate,
                EndDate = endDate,
                SearchText = searchText
            };
            var result = await _reportService.ExportSuggestionsToExcelAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting suggestions to Excel");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.SuggestionsExportError"), ex));
        }
    }
}
