using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.AI;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Core.Helpers;

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
    private readonly IAIReportService _aiReportService;
    private readonly IPdfService _pdfService;

    public ReportsApiController(
        IReportService reportService,
        ICustomerService customerService,
        ICustomerOrganizationService organizationService,
        IProjectService projectService,
        IUserService userService,
        ILogger<ReportsApiController> logger,
        ILocalizationService localizationService,
        IAIReportService aiReportService,
        IPdfService pdfService,
        IConfiguration configuration) : base(configuration)
    {
        _reportService = reportService;
        _customerService = customerService;
        _organizationService = organizationService;
        _projectService = projectService;
        _userService = userService;
        _logger = logger;
        _localizationService = localizationService;
        _aiReportService = aiReportService;
        _pdfService = pdfService;
    }

    /// <summary>
    /// Filtreler için lookup verileri (müşteri, organizasyon, proje, değerlendirici)
    /// </summary>
    [HttpGet("lookups")]
    public async Task<IActionResult> GetLookups([FromQuery] List<int>? customerIds = null)
    {
        try
        {
            var customers = await _customerService.GetListAsync(includeInactive: false);
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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.NotFound")));

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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.NotFound")));

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
    /// Ziyaret Denetleme Raporu - Excel export
    /// </summary>
    [HttpPost("export/visit-audit")]
    public async Task<IActionResult> ExportVisitAuditReport([FromBody] ReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.ExportVisitAuditReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting visit audit report to Excel");
            return StatusCode(500, CreateErrorResponse("Ziyaret denetleme raporu oluşturulurken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Müşteri Ziyaret Değerlendirme Raporu - Excel export
    /// </summary>
    [HttpPost("export/visit-customer-evaluation")]
    public async Task<IActionResult> ExportVisitCustomerEvaluationReport([FromBody] ReportFilterDto filter)
    {
        try
        {
            var result = await _reportService.ExportVisitCustomerEvaluationReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting visit customer evaluation report to Excel");
            return StatusCode(500, CreateErrorResponse("Müşteri ziyaret değerlendirme raporu oluşturulurken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Ziyaret Değerlendirme Detayı Excel export
    /// </summary>
    [HttpGet("evaluations/{evaluationId}/visit-export")]
    public async Task<IActionResult> ExportVisitEvaluationDetail(int evaluationId)
    {
        try
        {
            var result = await _reportService.ExportVisitEvaluationDetailToExcelAsync(evaluationId);
            if (result == null)
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Evaluation.NotFound")));

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting visit evaluation detail {EvaluationId}", evaluationId);
            return StatusCode(500, CreateErrorResponse("Ziyaret değerlendirme detayı export edilirken hata oluştu.", ex));
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

            var result = await _reportService.ExportPersonnelReportCardToExcelAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting personnel report card for {PersonnelId}", personnelId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.ReportCardExportError"), ex));
        }
    }

    /// <summary>
    /// Temsilci Karnesi PDF export (PdfService kullanarak)
    /// </summary>
    [HttpGet("personnel-report-card/{personnelId:int}/export-pdf")]
    public async Task<IActionResult> ExportPersonnelReportCardToPdf(
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

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var report = await _reportService.GetPersonnelReportCardAsync(filter);
            if (report == null)
                return NotFound(CreateErrorResponse("Karne verisi bulunamadı."));

            var html = GeneratePersonnelReportCardHtml(report);
            var pdfBytes = await _pdfService.GeneratePdfFromHtmlAsync(html);

            var fileName = $"TemsilciKarnesi_{report.PersonnelName.Replace(" ", "_")}_{TurkeyTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting personnel report card PDF for {PersonnelId}", personnelId);
            return StatusCode(500, CreateErrorResponse("Temsilci karnesi PDF oluşturulurken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Temsilci Karnesi Word export
    /// </summary>
    [HttpGet("personnel-report-card/{personnelId:int}/export-word")]
    public async Task<IActionResult> ExportPersonnelReportCardToWord(
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

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.ExportPersonnelReportCardToWordAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting personnel report card to Word for {PersonnelId}", personnelId);
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
        [FromQuery] int? projectId = null,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // Hem projectId hem projectIds destekle
            var effectiveProjectId = projectId ?? projectIds?.FirstOrDefault();
            var result = await _reportService.GetRecentSurveyResponsesAsync(count, effectiveProjectId, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading recent survey responses");
            return StatusCode(500, CreateErrorResponse("Son anket yanıtları yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Tüm Anket Yanıtları Excel Export (2 Sheet: Yanıtlar + Cevap Detayları)
    /// </summary>
    [HttpGet("survey-responses/export")]
    public async Task<IActionResult> ExportSurveyResponses([FromQuery] int? projectId = null)
    {
        try
        {
            var result = await _reportService.ExportSurveyResponsesToExcelAsync(projectId);
            if (result == null)
                return NotFound(CreateErrorResponse("Veri bulunamadı."));

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting survey responses for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse("Anket yanıtları export edilirken hata oluştu.", ex));
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
        [FromQuery] int? projectId = null,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            // Hem projectId hem projectIds destekle
            var effectiveProjectId = projectId ?? projectIds?.FirstOrDefault();
            var result = await _reportService.GetSurveyQuestionScoreDistributionAsync(effectiveProjectId, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading survey question score distribution");
            return StatusCode(500, CreateErrorResponse("Soru puan dağılımı yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Genel Soru Puan Dağılımı Excel Export
    /// </summary>
    [HttpGet("survey-question-distribution/export")]
    public async Task<IActionResult> ExportSurveyQuestionScoreDistribution([FromQuery] int? projectId = null)
    {
        try
        {
            if (!projectId.HasValue)
                return BadRequest(CreateErrorResponse("Proje seçimi zorunludur."));

            var result = await _reportService.ExportSurveyQuestionDistributionToExcelAsync(projectId.Value);
            if (result == null)
                return NotFound(CreateErrorResponse("Proje bulunamadı veya veri yok."));

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting survey question distribution for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse("Soru puan dağılımı export edilirken hata oluştu.", ex));
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

    // ===== AI RAPOR ENDPOINT'LERİ =====

    /// <summary>
    /// AI destekli rapor oluştur (Gemini API)
    /// </summary>
    [HttpPost("ai/generate")]
    public async Task<IActionResult> GenerateAIReport([FromBody] AIReportRequestDto request)
    {
        try
        {
            var result = await _aiReportService.GenerateReportAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI rapor oluşturma hatası: CustomerId={CustomerId}", request.CustomerId);
            return StatusCode(500, CreateErrorResponse("AI rapor oluşturulurken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// AI raporu için veri topla (önizleme/debug için)
    /// </summary>
    [HttpPost("ai/collect-data")]
    public async Task<IActionResult> CollectAIReportData([FromBody] AIReportRequestDto request)
    {
        try
        {
            var result = await _aiReportService.CollectReportDataAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AI rapor verisi toplama hatası: CustomerId={CustomerId}", request.CustomerId);
            return StatusCode(500, CreateErrorResponse("Rapor verisi toplanırken hata oluştu.", ex));
        }
    }

    // ===== ENNEAGRAM SONUÇLARI RAPORU =====

    /// <summary>
    /// Enneagram Projeleri Listesi (Enneagram checklist tipi kullanan projeler)
    /// </summary>
    [HttpGet("enneagram-projects")]
    public async Task<IActionResult> GetEnneagramProjects()
    {
        try
        {
            var result = await _reportService.GetEnneagramProjectsAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Enneagram projects");
            return StatusCode(500, CreateErrorResponse("Enneagram projeleri yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Enneagram Sonuçları Listesi (filtrelenebilir)
    /// </summary>
    [HttpGet("enneagram-results")]
    public async Task<IActionResult> GetEnneagramResults(
        [FromQuery] List<int>? projectIds,
        [FromQuery] string? searchTerm,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var filter = new EnneagramFilterDto
            {
                ProjectIds = projectIds,
                SearchTerm = searchTerm,
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

            var (results, summary, totalCount) = await _reportService.GetEnneagramResultsAsync(filter);

            return Ok(new
            {
                results,
                summary,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Enneagram results");
            return StatusCode(500, CreateErrorResponse("Enneagram sonuçları yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Enneagram Sonuç Detayı (kişilik tipi puanlarıyla)
    /// </summary>
    [HttpGet("enneagram-results/{evaluationId}")]
    public async Task<IActionResult> GetEnneagramResultDetail(int evaluationId)
    {
        try
        {
            var result = await _reportService.GetEnneagramResultDetailAsync(evaluationId);
            if (result == null)
                return NotFound(CreateErrorResponse("Sonuç bulunamadı."));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Enneagram result detail for {EvaluationId}", evaluationId);
            return StatusCode(500, CreateErrorResponse("Enneagram sonuç detayı yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Enneagram Sonuçları Excel Export
    /// </summary>
    [HttpGet("enneagram-results/export")]
    public async Task<IActionResult> ExportEnneagramResults(
        [FromQuery] List<int>? projectIds,
        [FromQuery] string? searchTerm,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var filter = new EnneagramFilterDto
            {
                ProjectIds = projectIds,
                SearchTerm = searchTerm
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.ExportEnneagramResultsToExcelAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting Enneagram results to Excel");
            return StatusCode(500, CreateErrorResponse("Enneagram sonuçları export edilirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Enneagram Proje Bazlı Kişilik Tipi Dağılımı
    /// </summary>
    [HttpGet("enneagram-distribution/{projectId}")]
    public async Task<IActionResult> GetEnneagramDistribution(int projectId)
    {
        try
        {
            var result = await _reportService.GetEnneagramDistributionAsync(projectId);
            if (result == null)
                return NotFound(CreateErrorResponse("Proje bulunamadı veya Enneagram projesi değil."));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading Enneagram distribution for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse("Enneagram dağılımı yüklenirken hata oluştu.", ex));
        }
    }

    // ===== ŞUBE KARNESİ =====

    [HttpGet("dealer-report-card/customers")]
    public async Task<IActionResult> GetDealerReportCardCustomers()
    {
        try
        {
            var customers = await _reportService.GetCustomersWithDealersAsync();
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customers with dealers");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.LoadError"), ex));
        }
    }

    [HttpGet("dealer-list")]
    public async Task<IActionResult> GetDealerList([FromQuery] List<int>? customerIds = null)
    {
        try
        {
            var customerId = customerIds?.FirstOrDefault();
            var dealers = await _reportService.GetDealerListAsync(customerId);
            return Ok(dealers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dealer list for report card");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.LoadError"), ex));
        }
    }

    [HttpGet("dealer-projects/{dealerId:int}")]
    public async Task<IActionResult> GetDealerProjects(int dealerId)
    {
        try
        {
            var projects = await _reportService.GetDealerProjectsAsync(dealerId);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading projects for dealer {DealerId}", dealerId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Report.LoadError"), ex));
        }
    }

    [HttpGet("dealer-report-card/{dealerId:int}")]
    public async Task<IActionResult> GetDealerReportCard(
        int dealerId,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var filter = new DealerReportCardFilterDto
            {
                DealerId = dealerId,
                ProjectIds = projectIds
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.GetDealerReportCardAsync(filter);
            if (result == null)
                return NotFound(CreateErrorResponse("Şube bulunamadı."));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dealer report card for {DealerId}", dealerId);
            return StatusCode(500, CreateErrorResponse("Şube karnesi yüklenirken hata oluştu.", ex));
        }
    }

    [HttpGet("dealer-report-card/{dealerId:int}/export")]
    public async Task<IActionResult> ExportDealerReportCard(
        int dealerId,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var filter = new DealerReportCardFilterDto
            {
                DealerId = dealerId,
                ProjectIds = projectIds
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.ExportDealerReportCardToExcelAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting dealer report card for {DealerId}", dealerId);
            return StatusCode(500, CreateErrorResponse("Şube karnesi export edilirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Şube Karnesi PDF export (PdfService kullanarak)
    /// </summary>
    [HttpGet("dealer-report-card/{dealerId:int}/export-pdf")]
    public async Task<IActionResult> ExportDealerReportCardToPdf(
        int dealerId,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var filter = new DealerReportCardFilterDto
            {
                DealerId = dealerId,
                ProjectIds = projectIds
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var report = await _reportService.GetDealerReportCardAsync(filter);
            if (report == null)
                return NotFound(CreateErrorResponse("Şube bulunamadı."));

            var html = GenerateDealerReportCardHtml(report);
            var pdfBytes = await _pdfService.GeneratePdfFromHtmlAsync(html);

            var fileName = $"SubeKarnesi_{report.DealerName.Replace(" ", "_")}_{TurkeyTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting dealer report card PDF for {DealerId}", dealerId);
            return StatusCode(500, CreateErrorResponse("Şube karnesi PDF oluşturulurken hata oluştu.", ex));
        }
    }

    [HttpGet("dealer-report-card/{dealerId:int}/export-word")]
    public async Task<IActionResult> ExportDealerReportCardToWord(
        int dealerId,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var filter = new DealerReportCardFilterDto
            {
                DealerId = dealerId,
                ProjectIds = projectIds
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.ExportDealerReportCardToWordAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting dealer report card to Word for {DealerId}", dealerId);
            return StatusCode(500, CreateErrorResponse("Şube karnesi Word export edilirken hata oluştu.", ex));
        }
    }

    // ===== PDF HTML GENERATOR METOTLARI =====

    private string GeneratePersonnelReportCardHtml(PersonnelReportCardDto report)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 11pt; color: #333; margin: 20px; }");
        sb.AppendLine("h1 { color: #0d6efd; border-bottom: 2px solid #0d6efd; padding-bottom: 8px; }");
        sb.AppendLine("h2 { color: #495057; margin-top: 5px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-bottom: 15px; }");
        sb.AppendLine("th, td { border: 1px solid #dee2e6; padding: 6px 10px; text-align: left; }");
        sb.AppendLine("th { background-color: #f8f9fa; font-weight: 600; }");
        sb.AppendLine(".text-center { text-align: center; }");
        sb.AppendLine(".text-success { color: #198754; }");
        sb.AppendLine(".text-warning { color: #ffc107; }");
        sb.AppendLine(".text-danger { color: #dc3545; }");
        sb.AppendLine(".badge { display: inline-block; padding: 3px 8px; border-radius: 4px; color: #fff; font-size: 10pt; }");
        sb.AppendLine(".bg-success { background-color: #198754; }");
        sb.AppendLine(".bg-warning { background-color: #ffc107; color: #000; }");
        sb.AppendLine(".bg-danger { background-color: #dc3545; }");
        sb.AppendLine(".card { border: 1px solid #dee2e6; border-radius: 6px; margin-bottom: 15px; }");
        sb.AppendLine(".card-header { background-color: #f8f9fa; padding: 8px 12px; font-weight: 600; border-bottom: 1px solid #dee2e6; }");
        sb.AppendLine(".page-break { page-break-before: always; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine($"<h1>Temsilci Karnesi</h1>");
        sb.AppendLine($"<h2>{report.PersonnelName}</h2>");
        if (!string.IsNullOrEmpty(report.Title))
            sb.AppendLine($"<p><strong>Ünvan:</strong> {report.Title}</p>");

        // Özet
        sb.AppendLine("<div class='card'><div class='card-header'>Performans Özeti</div>");
        sb.AppendLine("<table>");
        sb.AppendLine($"<tr><td>Toplam Değerlendirme</td><td><strong>{report.TotalEvaluations}</strong></td></tr>");
        sb.AppendLine($"<tr><td>Ortalama Puan</td><td><strong class='{GetScoreClass(report.AverageScore)}'>{report.AverageScore:F1}%</strong></td></tr>");
        sb.AppendLine($"<tr><td>En Yüksek Puan</td><td class='text-success'>{report.BestScore:F1}%</td></tr>");
        sb.AppendLine($"<tr><td>En Düşük Puan</td><td class='text-danger'>{report.WorstScore:F1}%</td></tr>");
        sb.AppendLine($"<tr><td>Sarı Kart</td><td>{report.TotalYellowCards}</td></tr>");
        sb.AppendLine($"<tr><td>Kırmızı Kart</td><td>{report.TotalRedCards}</td></tr>");
        sb.AppendLine("</table></div>");

        // Aylık Trend
        if (report.MonthlyTrend.Any())
        {
            sb.AppendLine("<div class='card'><div class='card-header'>Aylık Performans</div>");
            sb.AppendLine("<table><thead><tr><th>Dönem</th><th class='text-center'>Değerlendirme</th><th class='text-center'>Ort. Puan</th><th class='text-center'>S.Kart</th><th class='text-center'>K.Kart</th></tr></thead><tbody>");
            foreach (var trend in report.MonthlyTrend)
            {
                sb.AppendLine($"<tr><td>{trend.MonthName}</td><td class='text-center'>{trend.EvaluationCount}</td><td class='text-center'><span class='badge {GetBadgeClass(trend.AverageScore)}'>{trend.AverageScore:F1}%</span></td><td class='text-center'>{trend.YellowCards}</td><td class='text-center'>{trend.RedCards}</td></tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        // Grup Performansı
        if (report.GroupPerformances.Any())
        {
            sb.AppendLine("<div class='card'><div class='card-header'>Grup Performansı</div>");
            sb.AppendLine("<table><thead><tr><th>Grup</th><th class='text-center'>Başarı</th></tr></thead><tbody>");
            foreach (var group in report.GroupPerformances)
            {
                sb.AppendLine($"<tr><td>{group.GroupName}</td><td class='text-center'><span class='badge {GetBadgeClass(group.PercentageScore)}'>{group.PercentageScore:F1}%</span></td></tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        // Güçlü/Zayıf Yönler
        if (report.Strengths.Any() || report.Weaknesses.Any())
        {
            sb.AppendLine("<div class='card'><div class='card-header'>Güçlü ve Zayıf Yönler</div>");
            if (report.Strengths.Any())
            {
                sb.AppendLine("<h3 style='color:#198754;margin:10px 12px 5px;'>Güçlü Yönler</h3><ul style='margin:0 12px 10px;'>");
                foreach (var s in report.Strengths.Take(5))
                    sb.AppendLine($"<li><span class='badge bg-success'>{s.PercentageScore:F0}%</span> {s.QuestionText}</li>");
                sb.AppendLine("</ul>");
            }
            if (report.Weaknesses.Any())
            {
                sb.AppendLine("<h3 style='color:#dc3545;margin:10px 12px 5px;'>Geliştirilmeli</h3><ul style='margin:0 12px 10px;'>");
                foreach (var w in report.Weaknesses.Take(5))
                    sb.AppendLine($"<li><span class='badge bg-danger'>{w.PercentageScore:F0}%</span> {w.QuestionText}</li>");
                sb.AppendLine("</ul>");
            }
            sb.AppendLine("</div>");
        }

        // Son Değerlendirmeler
        if (report.RecentEvaluations.Any())
        {
            sb.AppendLine("<div class='page-break'></div>");
            sb.AppendLine("<div class='card'><div class='card-header'>Son Değerlendirmeler</div>");
            sb.AppendLine("<table><thead><tr><th>Tarih</th><th>Proje</th><th>Kontrol Listesi</th><th class='text-center'>Puan</th><th class='text-center'>Kartlar</th></tr></thead><tbody>");
            foreach (var eval in report.RecentEvaluations.Take(20))
            {
                var cards = "";
                if (eval.YellowCards > 0) cards += $"<span class='badge bg-warning'>{eval.YellowCards}</span> ";
                if (eval.RedCards > 0) cards += $"<span class='badge bg-danger'>{eval.RedCards}</span>";
                if (string.IsNullOrEmpty(cards)) cards = "-";
                sb.AppendLine($"<tr><td>{eval.EvaluationDate?.ToString("dd.MM.yyyy") ?? "-"}</td><td>{eval.ProjectName}</td><td>{eval.ChecklistName}</td><td class='text-center'><span class='badge {GetBadgeClass(eval.ScorePercentage)}'>{eval.ScorePercentage:F1}%</span></td><td class='text-center'>{cards}</td></tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        sb.AppendLine($"<p style='text-align:right;font-size:9pt;color:#999;margin-top:20px;'>Oluşturulma: {TurkeyTime.Now:dd.MM.yyyy HH:mm}</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private string GenerateDealerReportCardHtml(DealerReportCardDto report)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 11pt; color: #333; margin: 20px; }");
        sb.AppendLine("h1 { color: #0d6efd; border-bottom: 2px solid #0d6efd; padding-bottom: 8px; }");
        sb.AppendLine("h2 { color: #495057; margin-top: 5px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-bottom: 15px; }");
        sb.AppendLine("th, td { border: 1px solid #dee2e6; padding: 6px 10px; text-align: left; }");
        sb.AppendLine("th { background-color: #f8f9fa; font-weight: 600; }");
        sb.AppendLine(".text-center { text-align: center; }");
        sb.AppendLine(".text-success { color: #198754; }");
        sb.AppendLine(".text-warning { color: #ffc107; }");
        sb.AppendLine(".text-danger { color: #dc3545; }");
        sb.AppendLine(".badge { display: inline-block; padding: 3px 8px; border-radius: 4px; color: #fff; font-size: 10pt; }");
        sb.AppendLine(".bg-success { background-color: #198754; }");
        sb.AppendLine(".bg-warning { background-color: #ffc107; color: #000; }");
        sb.AppendLine(".bg-danger { background-color: #dc3545; }");
        sb.AppendLine(".card { border: 1px solid #dee2e6; border-radius: 6px; margin-bottom: 15px; }");
        sb.AppendLine(".card-header { background-color: #f8f9fa; padding: 8px 12px; font-weight: 600; border-bottom: 1px solid #dee2e6; }");
        sb.AppendLine(".page-break { page-break-before: always; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine($"<h1>Şube Karnesi</h1>");
        sb.AppendLine($"<h2>{report.DealerName}</h2>");
        if (!string.IsNullOrEmpty(report.DealerCode))
            sb.AppendLine($"<p><strong>Şube Kodu:</strong> {report.DealerCode}</p>");
        if (!string.IsNullOrEmpty(report.City))
            sb.AppendLine($"<p><strong>Konum:</strong> {report.City}{(string.IsNullOrEmpty(report.District) ? "" : " / " + report.District)}</p>");

        // Özet
        sb.AppendLine("<div class='card'><div class='card-header'>Performans Özeti</div>");
        sb.AppendLine("<table>");
        sb.AppendLine($"<tr><td>Toplam Değerlendirme</td><td><strong>{report.TotalEvaluations}</strong></td></tr>");
        sb.AppendLine($"<tr><td>Ortalama Puan</td><td><strong class='{GetScoreClass(report.AverageScore)}'>{report.AverageScore:F1}%</strong></td></tr>");
        sb.AppendLine($"<tr><td>En Yüksek Puan</td><td class='text-success'>{report.BestScore:F1}%</td></tr>");
        sb.AppendLine($"<tr><td>En Düşük Puan</td><td class='text-danger'>{report.WorstScore:F1}%</td></tr>");
        sb.AppendLine($"<tr><td>Sarı Kart</td><td>{report.TotalYellowCards}</td></tr>");
        sb.AppendLine($"<tr><td>Kırmızı Kart</td><td>{report.TotalRedCards}</td></tr>");
        sb.AppendLine("</table></div>");

        // Aylık Trend
        if (report.MonthlyTrend.Any())
        {
            sb.AppendLine("<div class='card'><div class='card-header'>Aylık Performans</div>");
            sb.AppendLine("<table><thead><tr><th>Dönem</th><th class='text-center'>Değerlendirme</th><th class='text-center'>Ort. Puan</th><th class='text-center'>S.Kart</th><th class='text-center'>K.Kart</th></tr></thead><tbody>");
            foreach (var trend in report.MonthlyTrend)
            {
                sb.AppendLine($"<tr><td>{trend.MonthName}</td><td class='text-center'>{trend.EvaluationCount}</td><td class='text-center'><span class='badge {GetBadgeClass(trend.AverageScore)}'>{trend.AverageScore:F1}%</span></td><td class='text-center'>{trend.YellowCards}</td><td class='text-center'>{trend.RedCards}</td></tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        // Grup Performansı
        if (report.GroupPerformances.Any())
        {
            sb.AppendLine("<div class='card'><div class='card-header'>Grup Performansı</div>");
            sb.AppendLine("<table><thead><tr><th>Grup</th><th class='text-center'>Başarı</th></tr></thead><tbody>");
            foreach (var group in report.GroupPerformances)
            {
                sb.AppendLine($"<tr><td>{group.GroupName}</td><td class='text-center'><span class='badge {GetBadgeClass(group.PercentageScore)}'>{group.PercentageScore:F1}%</span></td></tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        // Güçlü/Zayıf Yönler
        if (report.Strengths.Any() || report.Weaknesses.Any())
        {
            sb.AppendLine("<div class='card'><div class='card-header'>Güçlü ve Zayıf Yönler</div>");
            if (report.Strengths.Any())
            {
                sb.AppendLine("<h3 style='color:#198754;margin:10px 12px 5px;'>Güçlü Yönler</h3><ul style='margin:0 12px 10px;'>");
                foreach (var s in report.Strengths.Take(5))
                    sb.AppendLine($"<li><span class='badge bg-success'>{s.PercentageScore:F0}%</span> {s.QuestionText}</li>");
                sb.AppendLine("</ul>");
            }
            if (report.Weaknesses.Any())
            {
                sb.AppendLine("<h3 style='color:#dc3545;margin:10px 12px 5px;'>Geliştirilmeli</h3><ul style='margin:0 12px 10px;'>");
                foreach (var w in report.Weaknesses.Take(5))
                    sb.AppendLine($"<li><span class='badge bg-danger'>{w.PercentageScore:F0}%</span> {w.QuestionText}</li>");
                sb.AppendLine("</ul>");
            }
            sb.AppendLine("</div>");
        }

        // Son Değerlendirmeler
        if (report.RecentEvaluations.Any())
        {
            sb.AppendLine("<div class='page-break'></div>");
            sb.AppendLine("<div class='card'><div class='card-header'>Son Değerlendirmeler</div>");
            sb.AppendLine("<table><thead><tr><th>Tarih</th><th>Proje</th><th>Kontrol Listesi</th><th class='text-center'>Puan</th><th class='text-center'>Kartlar</th><th>Personel</th></tr></thead><tbody>");
            foreach (var eval in report.RecentEvaluations.Take(20))
            {
                var cards = "";
                if (eval.YellowCards > 0) cards += $"<span class='badge bg-warning'>{eval.YellowCards}</span> ";
                if (eval.RedCards > 0) cards += $"<span class='badge bg-danger'>{eval.RedCards}</span>";
                if (string.IsNullOrEmpty(cards)) cards = "-";
                sb.AppendLine($"<tr><td>{eval.EvaluationDate?.ToString("dd.MM.yyyy") ?? "-"}</td><td>{eval.ProjectName}</td><td>{eval.ChecklistName}</td><td class='text-center'><span class='badge {GetBadgeClass(eval.ScorePercentage)}'>{eval.ScorePercentage:F1}%</span></td><td class='text-center'>{cards}</td><td>{eval.PersonnelName ?? "-"}</td></tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        sb.AppendLine($"<p style='text-align:right;font-size:9pt;color:#999;margin-top:20px;'>Oluşturulma: {TurkeyTime.Now:dd.MM.yyyy HH:mm}</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    private string GetScoreClass(decimal score)
    {
        if (score >= 80) return "text-success";
        if (score >= 60) return "text-warning";
        return "text-danger";
    }

    private string GetBadgeClass(decimal score)
    {
        if (score >= 80) return "bg-success";
        if (score >= 60) return "bg-warning";
        return "bg-danger";
    }
}
