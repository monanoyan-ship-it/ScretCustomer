using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = "Admin,TeamLeader")]
public class ReportsApiController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportsApiController> _logger;

    public ReportsApiController(IReportService reportService, ILogger<ReportsApiController> logger)
    {
        _reportService = reportService;
        _logger = logger;
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
            return StatusCode(500, new { message = "Rapor yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Değerlendirme detayı
    /// </summary>
    [HttpGet("evaluations/{evaluationId}")]
    public async Task<IActionResult> GetEvaluationDetail(Guid evaluationId)
    {
        try
        {
            var result = await _reportService.GetEvaluationDetailAsync(evaluationId);
            if (result == null)
                return NotFound(new { message = "Değerlendirme bulunamadı." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading evaluation detail {EvaluationId}", evaluationId);
            return StatusCode(500, new { message = "Değerlendirme detayı yüklenirken bir hata oluştu." });
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
            return StatusCode(500, new { message = "Özet rapor yüklenirken bir hata oluştu." });
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
            return StatusCode(500, new { message = "Excel dosyası oluşturulurken bir hata oluştu." });
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
            return StatusCode(500, new { message = "Detaylı Excel dosyası oluşturulurken bir hata oluştu." });
        }
    }

    // ===== CEZALI KL RAPORU =====

    /// <summary>
    /// Cezalı KL Raporu
    /// </summary>
    [HttpGet("penalties")]
    public async Task<IActionResult> GetPenaltiesReport(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? branchId,
        [FromQuery] string? penaltyType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var filter = new PenaltyFilterDto
            {
                ProjectId = projectId,
                BranchId = branchId,
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
            return StatusCode(500, new { message = "Cezalı KL raporu yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Cezalı KL Raporu Excel Export
    /// </summary>
    [HttpGet("penalties/export")]
    public async Task<IActionResult> ExportPenaltiesToExcel(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? branchId,
        [FromQuery] string? penaltyType,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var filter = new PenaltyFilterDto
            {
                ProjectId = projectId,
                BranchId = branchId,
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
            return StatusCode(500, new { message = "Cezalı KL raporu Excel dosyası oluşturulurken bir hata oluştu." });
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
            return StatusCode(500, new { message = "Personel listesi yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Temsilci Karnesi raporunu getirir
    /// </summary>
    [HttpGet("personnel-report-card/{personnelId:guid}")]
    public async Task<IActionResult> GetPersonnelReportCard(
        Guid personnelId,
        [FromQuery] Guid? projectId,
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
                return NotFound(new { message = "Personel bulunamadı." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading personnel report card for {PersonnelId}", personnelId);
            return StatusCode(500, new { message = "Temsilci karnesi yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Temsilci Karnesi PDF/Excel export
    /// </summary>
    [HttpGet("personnel-report-card/{personnelId:guid}/export")]
    public async Task<IActionResult> ExportPersonnelReportCard(
        Guid personnelId,
        [FromQuery] Guid? projectId,
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
            return StatusCode(500, new { message = "Temsilci karnesi dışa aktarılırken bir hata oluştu." });
        }
    }

    // ===== ÖNERİLER RAPORU (Video 5-6) =====

    /// <summary>
    /// Öneriler Raporu - Tüm değerlendirmelerdeki önerilerin listesi
    /// </summary>
    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestionsReport(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? checklistId,
        [FromQuery] Guid? evaluatorId,
        [FromQuery] Guid? personnelId,
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
                BranchId = branchId,
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
            return StatusCode(500, new { message = "Öneriler raporu yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// En çok öneri yazılan sorular
    /// </summary>
    [HttpGet("suggestions/top-questions")]
    public async Task<IActionResult> GetTopSuggestedQuestions(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? checklistId,
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
            return StatusCode(500, new { message = "En çok öneri yazılan sorular yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Öneriler Raporu Excel Export
    /// </summary>
    [HttpGet("suggestions/export")]
    public async Task<IActionResult> ExportSuggestionsToExcel(
        [FromQuery] Guid? projectId,
        [FromQuery] Guid? branchId,
        [FromQuery] Guid? checklistId,
        [FromQuery] Guid? evaluatorId,
        [FromQuery] Guid? personnelId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] string? searchText)
    {
        try
        {
            var filter = new SuggestionsFilterDto
            {
                ProjectId = projectId,
                BranchId = branchId,
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
            return StatusCode(500, new { message = "Öneriler raporu Excel dosyası oluşturulurken bir hata oluştu." });
        }
    }
}
