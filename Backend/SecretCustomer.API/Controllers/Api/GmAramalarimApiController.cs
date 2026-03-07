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
    private readonly IAuditLogService _auditLogService;

    public GmAramalarimApiController(
        IGmService gmService,
        IAuditLogService auditLogService,
        IConfiguration configuration) : base(configuration)
    {
        _gmService = gmService;
        _auditLogService = auditLogService;
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
            await _auditLogService.LogErrorAsync("Aramalarım yüklenirken hata", "GmAramalarim", ex);
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
            await _auditLogService.LogErrorAsync("Dönemler yüklenirken hata", "GmAramalarim", ex);
            return StatusCode(500, CreateErrorResponse("Dönemler yüklenirken hata oluştu", ex));
        }
    }

    [HttpGet("tamamlanan")]
    public async Task<IActionResult> GetTamamlananAramalar(
        [FromQuery] List<int>? donemIds,
        [FromQuery] List<string>? firmaArama,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();
            var result = await _gmService.GetTamamlananAramalarAsync(userId, donemIds, firmaArama, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Tamamlanan aramalar yüklenirken hata", "GmAramalarim", ex);
            return StatusCode(500, CreateErrorResponse("Tamamlanan aramalar yüklenirken hata oluştu", ex));
        }
    }

    [HttpGet("dinlemelerim")]
    public async Task<IActionResult> GetDinlemelerim()
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var result = await _gmService.GetDinlemelerimAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Dinlemelerim yüklenirken hata", "GmAramalarim", ex);
            return StatusCode(500, CreateErrorResponse("Dinlemelerim yüklenirken hata oluştu", ex));
        }
    }

    [HttpGet("kupon-bekleyenler")]
    public async Task<IActionResult> GetKuponBekleyenler()
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var result = await _gmService.GetKuponBekleyenAtamalarAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Kupon bekleyenler yüklenirken hata", "GmAramalarim", ex);
            return StatusCode(500, CreateErrorResponse("Kupon bekleyenler yüklenirken hata oluştu", ex));
        }
    }

    [HttpPut("{atamaId}/kupon-kodu")]
    public async Task<IActionResult> EnterKuponKodu(int atamaId, [FromBody] EnterKuponKoduDto dto)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var result = await _gmService.EnterKuponKoduAsync(atamaId, userId, dto);
            if (result == null) return NotFound(new { message = "Atama bulunamadı veya size ait değil." });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Kupon kodu girilirken hata", "GmAramalarim", ex);
            return StatusCode(500, CreateErrorResponse("Kupon kodu girilirken hata oluştu", ex));
        }
    }

    [HttpPut("{atamaId}/guncelle")]
    public async Task<IActionResult> UpdateCompletedAtama(int atamaId, [FromBody] CompleteGmAtamaDto dto)
    {
        try
        {
            var userId = GetUserId();
            if (userId == 0) return Unauthorized();

            var result = await _gmService.UpdateCompletedAtamaAsync(atamaId, userId, dto);
            if (!result) return NotFound(new { message = "Atama bulunamadı veya size ait değil." });
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Atama güncellenirken hata", "GmAramalarim", ex);
            return StatusCode(500, CreateErrorResponse("Atama güncellenirken hata oluştu", ex));
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
            await _auditLogService.LogErrorAsync("Atama tamamlanırken hata", "GmAramalarim", ex);
            return StatusCode(500, CreateErrorResponse("Atama tamamlanırken hata oluştu", ex));
        }
    }
}
