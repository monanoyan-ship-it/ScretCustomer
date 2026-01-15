using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.PerformanceSettings;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

/// <summary>
/// Performans Ayarları API Controller
/// Proje tipi bazlı performans hedeflerini yönetir
/// </summary>
[Route("api/performance-settings")]
[ApiController]
[Authorize(Roles = "Admin")]
public class PerformanceSettingsApiController : BaseApiController
{
    private readonly IPerformanceSettingsService _performanceSettingsService;
    private readonly ILogger<PerformanceSettingsApiController> _logger;

    public PerformanceSettingsApiController(
        IPerformanceSettingsService performanceSettingsService,
        ILogger<PerformanceSettingsApiController> logger,
        IConfiguration configuration) : base(configuration)
    {
        _performanceSettingsService = performanceSettingsService;
        _logger = logger;
    }

    private string? GetCurrentUserId()
    {
        return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    /// <summary>
    /// Tüm proje tipleri için performans ayarlarını getirir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var result = await _performanceSettingsService.GetAllAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance settings");
            return StatusCode(500, CreateErrorResponse("Performans ayarları yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Belirli bir proje tipinin performans ayarlarını getirir
    /// </summary>
    [HttpGet("{projectTypeId}")]
    public async Task<IActionResult> GetByProjectType(int projectTypeId)
    {
        try
        {
            var result = await _performanceSettingsService.GetByProjectTypeIdAsync(projectTypeId);
            if (result == null)
                return NotFound(CreateErrorResponse("Proje tipi bulunamadı."));

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting performance settings for project type {ProjectTypeId}", projectTypeId);
            return StatusCode(500, CreateErrorResponse("Performans ayarı yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Performans ayarlarını kaydeder (varsa günceller, yoksa oluşturur)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SavePerformanceSettingsDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _performanceSettingsService.SaveAsync(dto, userId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving performance settings for project type {ProjectTypeId}", dto.ProjectTypeId);
            return StatusCode(500, CreateErrorResponse("Performans ayarı kaydedilirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Tüm proje tipleri için ayarları toplu kaydeder
    /// </summary>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkSave([FromBody] BulkSavePerformanceSettingsDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _performanceSettingsService.BulkSaveAsync(dto, userId);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk saving performance settings");
            return StatusCode(500, CreateErrorResponse("Performans ayarları kaydedilirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Performans ayarlarını siler
    /// </summary>
    [HttpDelete("{projectTypeId}")]
    public async Task<IActionResult> Delete(int projectTypeId)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _performanceSettingsService.DeleteAsync(projectTypeId, userId);
            if (!result)
                return NotFound(CreateErrorResponse("Performans ayarı bulunamadı."));

            return Ok(new { message = "Performans ayarı başarıyla silindi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting performance settings for project type {ProjectTypeId}", projectTypeId);
            return StatusCode(500, CreateErrorResponse("Performans ayarı silinirken hata oluştu.", ex));
        }
    }
}
