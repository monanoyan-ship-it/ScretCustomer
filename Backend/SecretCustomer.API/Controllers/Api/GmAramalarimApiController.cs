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
    public async Task<IActionResult> GetAramalarim([FromQuery] int? donemId)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var result = await _gmService.GetAramalarimAsync(userId, donemId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Aramalarım yüklenirken hata");
            return StatusCode(500, CreateErrorResponse("Aramalarım yüklenirken hata oluştu", ex));
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
