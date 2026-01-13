using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.DealerRequest;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/dealer-requests")]
[Authorize]
public class DealerRequestsApiController : BaseApiController
{
    private readonly IDealerRequestService _dealerRequestService;
    private readonly ILogger<DealerRequestsApiController> _logger;

    public DealerRequestsApiController(
        IDealerRequestService dealerRequestService,
        ILogger<DealerRequestsApiController> logger,
        IConfiguration configuration) : base(configuration)
    {
        _dealerRequestService = dealerRequestService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Tüm talepleri getir (Admin için)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,QualitySpecialist")]
    public async Task<IActionResult> GetAll([FromQuery] DealerRequestFilterDto filter)
    {
        try
        {
            var result = await _dealerRequestService.GetAllAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dealer requests");
            return StatusCode(500, CreateErrorResponse("Talepler yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Talep detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var request = await _dealerRequestService.GetByIdAsync(id);
            if (request == null)
                return NotFound(CreateErrorResponse("Talep bulunamadı"));

            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dealer request {Id}", id);
            return StatusCode(500, CreateErrorResponse("Talep yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Kendi taleplerim (FieldWorker için)
    /// </summary>
    [HttpGet("my")]
    [Authorize(Roles = "FieldWorker,Admin,QualitySpecialist")]
    public async Task<IActionResult> GetMyRequests()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(CreateErrorResponse("Kullanıcı kimliği bulunamadı"));

            var requests = await _dealerRequestService.GetMyRequestsAsync(userId);
            return Ok(requests);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my dealer requests");
            return StatusCode(500, CreateErrorResponse("Talepler yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Yeni talep oluştur (FieldWorker için)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "FieldWorker,Admin,QualitySpecialist")]
    public async Task<IActionResult> Create([FromBody] CreateDealerRequestDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(CreateErrorResponse("Kullanıcı kimliği bulunamadı"));

            var request = await _dealerRequestService.CreateAsync(dto, userId);
            return CreatedAtAction(nameof(GetById), new { id = request.Id }, request);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating dealer request");
            return StatusCode(500, CreateErrorResponse("Talep oluşturulurken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Talebi onayla (Admin için)
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin,QualitySpecialist")]
    public async Task<IActionResult> Approve(int id, [FromBody] ProcessDealerRequestDto? dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(CreateErrorResponse("Kullanıcı kimliği bulunamadı"));

            var processDto = dto ?? new ProcessDealerRequestDto();
            processDto.Approve = true;

            var result = await _dealerRequestService.ProcessAsync(id, processDto, userId);
            if (result == null)
                return NotFound(CreateErrorResponse("Talep bulunamadı"));

            return Ok(new { message = "Talep başarıyla onaylandı", request = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving dealer request {Id}", id);
            return StatusCode(500, CreateErrorResponse("Talep onaylanırken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Talebi reddet (Admin için)
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin,QualitySpecialist")]
    public async Task<IActionResult> Reject(int id, [FromBody] ProcessDealerRequestDto? dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == 0)
                return Unauthorized(CreateErrorResponse("Kullanıcı kimliği bulunamadı"));

            var processDto = dto ?? new ProcessDealerRequestDto();
            processDto.Approve = false;

            var result = await _dealerRequestService.ProcessAsync(id, processDto, userId);
            if (result == null)
                return NotFound(CreateErrorResponse("Talep bulunamadı"));

            return Ok(new { message = "Talep reddedildi", request = result });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting dealer request {Id}", id);
            return StatusCode(500, CreateErrorResponse("Talep reddedilirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Talebi sil
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _dealerRequestService.DeleteAsync(id);
            if (!result)
                return NotFound(CreateErrorResponse("Talep bulunamadı"));

            return Ok(new { message = "Talep başarıyla silindi" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting dealer request {Id}", id);
            return StatusCode(500, CreateErrorResponse("Talep silinirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Bekleyen talep sayısı
    /// </summary>
    [HttpGet("pending-count")]
    [Authorize(Roles = "Admin,QualitySpecialist")]
    public async Task<IActionResult> GetPendingCount()
    {
        try
        {
            var count = await _dealerRequestService.GetPendingCountAsync();
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting pending count");
            return StatusCode(500, CreateErrorResponse("Bekleyen talep sayısı alınırken hata oluştu", ex));
        }
    }
}
