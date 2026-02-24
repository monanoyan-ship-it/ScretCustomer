using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.GolgeMusteri;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/gm/aramalarim")]
[Authorize]
public class GmAramalarimApiController : BaseApiController
{
    private readonly IGmService _gmService;
    private readonly ILogger<GmAramalarimApiController> _logger;

    public GmAramalarimApiController(
        IGmService gmService,
        ILogger<GmAramalarimApiController> logger,
        IConfiguration configuration) : base(configuration)
    {
        _gmService = gmService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst("UserId")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    [HttpGet]
    public async Task<IActionResult> GetAramalarim(
        [FromQuery] List<int>? donemIds,
        [FromQuery] List<int>? durumIds,
        [FromQuery] List<string>? firmaArama,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var result = await _gmService.GetAramalarimAsync(userId, donemIds, durumIds, firmaArama, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aramalarım yüklenirken hata");
            return StatusCode(500, CreateErrorResponse("Aramalarım yüklenirken hata oluştu", ex));
        }
    }

    [HttpGet("donemler")]
    public async Task<IActionResult> GetDonemler()
    {
        try
        {
            var result = await _gmService.GetDonemlerAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönemler yüklenirken hata");
            return StatusCode(500, CreateErrorResponse("Dönemler yüklenirken hata oluştu", ex));
        }
    }

    [HttpGet("tamamlanan")]
    public async Task<IActionResult> GetTamamlananAramalar(
        [FromQuery] List<int>? donemIds,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var result = await _gmService.GetTamamlananAramalarAsync(donemIds, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tamamlanan aramalar yüklenirken hata");
            return StatusCode(500, CreateErrorResponse("Tamamlanan aramalar yüklenirken hata oluştu", ex));
        }
    }

    [HttpPost("{atamaId}/tamamla")]
    public async Task<IActionResult> CompleteAtama(int atamaId, [FromBody] CompleteGmAtamaDto dto)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var result = await _gmService.CompleteAtamaAsync(atamaId, userId, dto);
            if (!result) return NotFound(new { message = "Atama bulunamadı veya size ait değil." });
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Atama tamamlanırken hata");
            return StatusCode(500, CreateErrorResponse("Atama tamamlanırken hata oluştu", ex));
        }
    }
}
