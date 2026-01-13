using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.FieldWorker;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/fieldworker")]
[Authorize(Roles = "FieldWorker,Admin,QualitySpecialist")]
public class FieldWorkerApiController : BaseApiController
{
    private readonly IFieldWorkerService _fieldWorkerService;
    private readonly ILogger<FieldWorkerApiController> _logger;

    public FieldWorkerApiController(
        IFieldWorkerService fieldWorkerService,
        ILogger<FieldWorkerApiController> logger,
        IConfiguration configuration) : base(configuration)
    {
        _fieldWorkerService = fieldWorkerService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Dashboard verileri
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(CreateErrorResponse("Kullanıcı kimliği bulunamadı"));

            var dashboard = await _fieldWorkerService.GetDashboardAsync(userId);
            return Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting fieldworker dashboard");
            return StatusCode(500, CreateErrorResponse("Dashboard yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Atanmış projeler
    /// </summary>
    [HttpGet("projects")]
    public async Task<IActionResult> GetAssignedProjects()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(CreateErrorResponse("Kullanıcı kimliği bulunamadı"));

            var projects = await _fieldWorkerService.GetAssignedProjectsAsync(userId);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assigned projects");
            return StatusCode(500, CreateErrorResponse("Projeler yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Erişilebilir bayiler
    /// </summary>
    [HttpGet("dealers")]
    public async Task<IActionResult> GetAccessibleDealers([FromQuery] int? projectId = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(CreateErrorResponse("Kullanıcı kimliği bulunamadı"));

            var dealers = await _fieldWorkerService.GetAccessibleDealersAsync(userId, projectId);
            return Ok(dealers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting accessible dealers");
            return StatusCode(500, CreateErrorResponse("Bayiler yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Ziyaretler listesi
    /// </summary>
    [HttpGet("visits")]
    public async Task<IActionResult> GetVisits([FromQuery] VisitFilterDto filter)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(CreateErrorResponse("Kullanıcı kimliği bulunamadı"));

            var result = await _fieldWorkerService.GetVisitsAsync(userId, filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visits");
            return StatusCode(500, CreateErrorResponse("Ziyaretler yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Ziyaret detayı
    /// </summary>
    [HttpGet("visits/{id}")]
    public async Task<IActionResult> GetVisit(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(CreateErrorResponse("Kullanıcı kimliği bulunamadı"));

            var visit = await _fieldWorkerService.GetVisitByIdAsync(userId, id);
            if (visit == null)
                return NotFound(CreateErrorResponse("Ziyaret bulunamadı"));

            return Ok(visit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visit {Id}", id);
            return StatusCode(500, CreateErrorResponse("Ziyaret yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Yeni ziyaret oluştur
    /// </summary>
    [HttpPost("visits")]
    public async Task<IActionResult> CreateVisit([FromBody] CreateVisitDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(CreateErrorResponse("Kullanıcı kimliği bulunamadı"));

            var evaluationId = await _fieldWorkerService.CreateVisitAsync(userId, dto);
            return Ok(new { evaluationId, message = dto.IsDraft ? "Ziyaret taslak olarak kaydedildi" : "Ziyaret başarıyla kaydedildi" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating visit");
            return StatusCode(500, CreateErrorResponse("Ziyaret oluşturulurken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Ziyareti güncelle
    /// </summary>
    [HttpPut("visits/{id}")]
    public async Task<IActionResult> UpdateVisit(int id, [FromBody] CreateVisitDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(CreateErrorResponse("Kullanıcı kimliği bulunamadı"));

            var result = await _fieldWorkerService.UpdateVisitAsync(userId, id, dto);
            if (!result)
                return NotFound(CreateErrorResponse("Ziyaret bulunamadı"));

            return Ok(new { message = dto.IsDraft ? "Ziyaret taslak olarak güncellendi" : "Ziyaret başarıyla güncellendi" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating visit {Id}", id);
            return StatusCode(500, CreateErrorResponse("Ziyaret güncellenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Yeni VisitId üret
    /// </summary>
    [HttpGet("generate-visit-id")]
    public async Task<IActionResult> GenerateVisitId()
    {
        try
        {
            var visitId = await _fieldWorkerService.GenerateVisitIdAsync();
            return Ok(new { visitId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating visit id");
            return StatusCode(500, CreateErrorResponse("VisitId üretilirken hata oluştu", ex));
        }
    }
}
