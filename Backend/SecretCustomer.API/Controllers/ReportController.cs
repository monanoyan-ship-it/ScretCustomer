using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReportController : ControllerBase
{
    private readonly IReportService _reportService;
    private readonly ILogger<ReportController> _logger;

    public ReportController(
        IReportService reportService,
        ILogger<ReportController> logger)
    {
        _reportService = reportService;
        _logger = logger;
    }

    /// <summary>
    /// Tek bir değerlendirme raporu getirir
    /// </summary>
    [HttpGet("evaluation/{evaluationId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<ActionResult<EvaluationReportDto>> GetEvaluationReport(Guid evaluationId)
    {
        try
        {
            var report = await _reportService.GetEvaluationReportAsync(evaluationId);
            if (report == null)
                return NotFound($"Evaluation with ID {evaluationId} not found");

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evaluation report {EvaluationId}", evaluationId);
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    /// <summary>
    /// Şube performans raporu getirir
    /// </summary>
    [HttpPost("branch/{branchId}/performance")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<ActionResult<BranchPerformanceReportDto>> GetBranchPerformanceReport(
        Guid branchId,
        [FromBody] ReportFilterDto? filter = null)
    {
        try
        {
            var report = await _reportService.GetBranchPerformanceReportAsync(branchId, filter);
            if (report == null)
                return NotFound($"Branch with ID {branchId} not found");

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branch performance report {BranchId}", branchId);
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    /// <summary>
    /// Proje raporu getirir
    /// </summary>
    [HttpGet("project/{projectId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<ActionResult<ProjectReportDto>> GetProjectReport(Guid projectId)
    {
        try
        {
            var report = await _reportService.GetProjectReportAsync(projectId);
            if (report == null)
                return NotFound($"Project with ID {projectId} not found");

            return Ok(report);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project report {ProjectId}", projectId);
            return StatusCode(500, "An error occurred while generating the report");
        }
    }

    /// <summary>
    /// Filtrelenmiş değerlendirme raporları getirir
    /// </summary>
    [HttpPost("evaluations")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<ActionResult<IEnumerable<EvaluationReportDto>>> GetEvaluationReports(
        [FromBody] ReportFilterDto filter)
    {
        try
        {
            var reports = await _reportService.GetEvaluationReportsAsync(filter);
            return Ok(reports);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting evaluation reports");
            return StatusCode(500, "An error occurred while generating the reports");
        }
    }
}
