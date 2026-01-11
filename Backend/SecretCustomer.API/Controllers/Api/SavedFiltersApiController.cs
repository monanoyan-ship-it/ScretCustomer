using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.SavedFilter;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/saved-filters")]
[Authorize]
public class SavedFiltersApiController : BaseApiController
{
    private readonly ISavedFilterService _savedFilterService;
    private readonly ILogger<SavedFiltersApiController> _logger;

    public SavedFiltersApiController(
        ISavedFilterService savedFilterService,
        ILogger<SavedFiltersApiController> logger,
        IConfiguration configuration) : base(configuration)
    {
        _savedFilterService = savedFilterService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Kullanıcı bilgisi alınamadı");
        return userId;
    }

    /// <summary>
    /// Belirli bir sayfa için kullanıcının kayıtlı filtrelerini getir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetByPage([FromQuery] string pageName)
    {
        try
        {
            if (string.IsNullOrEmpty(pageName))
                return BadRequest(CreateErrorResponse("Sayfa adı zorunludur"));

            var userId = GetCurrentUserId();
            var filters = await _savedFilterService.GetByUserAndPageAsync(userId, pageName);
            return Ok(filters);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading saved filters for page {PageName}", pageName);
            return StatusCode(500, CreateErrorResponse("Filtreler yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Yeni filtre kaydet
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSavedFilterDto dto)
    {
        try
        {
            if (string.IsNullOrEmpty(dto.Name))
                return BadRequest(CreateErrorResponse("Filtre adı zorunludur"));

            if (string.IsNullOrEmpty(dto.PageName))
                return BadRequest(CreateErrorResponse("Sayfa adı zorunludur"));

            var userId = GetCurrentUserId();
            var filter = await _savedFilterService.CreateAsync(userId, dto);
            return Ok(new { message = "Filtre başarıyla kaydedildi", filter });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating saved filter");
            return StatusCode(500, CreateErrorResponse("Filtre kaydedilirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Filtre sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            await _savedFilterService.DeleteAsync(id, userId);
            return Ok(new { message = "Filtre başarıyla silindi" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(CreateErrorResponse(ex.Message));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting saved filter {Id}", id);
            return StatusCode(500, CreateErrorResponse("Filtre silinirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Varsayılan filtre ayarla
    /// </summary>
    [HttpPost("{id}/set-default")]
    public async Task<IActionResult> SetDefault(int id, [FromQuery] string pageName)
    {
        try
        {
            if (string.IsNullOrEmpty(pageName))
                return BadRequest(CreateErrorResponse("Sayfa adı zorunludur"));

            var userId = GetCurrentUserId();
            await _savedFilterService.SetDefaultAsync(userId, pageName, id);
            return Ok(new { message = "Varsayılan filtre ayarlandı" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default filter {Id}", id);
            return StatusCode(500, CreateErrorResponse("Varsayılan filtre ayarlanırken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Varsayılan filtreyi kaldır (hepsini sıfırla)
    /// </summary>
    [HttpPost("clear-default")]
    public async Task<IActionResult> ClearDefault([FromQuery] string pageName)
    {
        try
        {
            if (string.IsNullOrEmpty(pageName))
                return BadRequest(CreateErrorResponse("Sayfa adı zorunludur"));

            var userId = GetCurrentUserId();
            // filterId=0 ile çağırınca sadece clear yapar
            await _savedFilterService.SetDefaultAsync(userId, pageName, 0);
            return Ok(new { message = "Varsayılan filtre kaldırıldı" });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing default filter for page {PageName}", pageName);
            return StatusCode(500, CreateErrorResponse("Varsayılan filtre kaldırılırken hata oluştu", ex));
        }
    }
}
