using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.PersonnelRequest;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/personnel-requests")]
[ApiController]
[Authorize]
public class PersonnelRequestsApiController : BaseApiController
{
    private readonly IPersonnelRequestService _personnelRequestService;
    private readonly ILogger<PersonnelRequestsApiController> _logger;

    public PersonnelRequestsApiController(
        IPersonnelRequestService personnelRequestService,
        ILogger<PersonnelRequestsApiController> logger,
        IConfiguration configuration) : base(configuration)
    {
        _personnelRequestService = personnelRequestService;
        _logger = logger;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    /// <summary>
    /// Personel taleplerini listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PersonnelRequestFilterDto filter)
    {
        try
        {
            var (items, totalCount) = await _personnelRequestService.GetAllAsync(filter);
            return Ok(new
            {
                items,
                totalCount,
                page = filter.Page,
                pageSize = filter.PageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personnel requests");
            return StatusCode(500, CreateErrorResponse("Personel talepleri yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// ID ile talep getir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var request = await _personnelRequestService.GetByIdAsync(id);
            if (request == null)
                return NotFound(CreateErrorResponse("Talep bulunamadı"));

            return Ok(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personnel request: {Id}", id);
            return StatusCode(500, CreateErrorResponse("Talep yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Yeni personel talebi oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePersonnelRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var result = await _personnelRequestService.CreateAsync(dto, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating personnel request");
            return StatusCode(500, CreateErrorResponse("Personel talebi oluşturulurken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Talebi onayla (Admin)
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApprovePersonnelRequestDto dto)
    {
        try
        {
            dto.Id = id;
            var userId = GetCurrentUserId();
            var result = await _personnelRequestService.ApproveAsync(dto, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving personnel request: {Id}", id);
            return StatusCode(500, CreateErrorResponse("Talep onaylanırken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Talebi reddet (Admin)
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectPersonnelRequestDto dto)
    {
        try
        {
            dto.Id = id;
            var userId = GetCurrentUserId();
            var result = await _personnelRequestService.RejectAsync(dto, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error rejecting personnel request: {Id}", id);
            return StatusCode(500, CreateErrorResponse("Talep reddedilirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Organizasyona göre süpervizör listesi (onay ekranı için)
    /// </summary>
    [HttpGet("supervisors/{organizationId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetSupervisors(int organizationId)
    {
        try
        {
            var supervisors = await _personnelRequestService.GetSupervisorsForOrganizationAsync(organizationId);
            return Ok(supervisors.Select(s => new { id = s.Id, name = s.FullName }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting supervisors for organization: {Id}", organizationId);
            return StatusCode(500, CreateErrorResponse("Süpervizörler yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Durum sayıları (özet kartları için)
    /// </summary>
    [HttpGet("counts")]
    public async Task<IActionResult> GetStatusCounts()
    {
        try
        {
            var counts = await _personnelRequestService.GetStatusCountsAsync();
            return Ok(new
            {
                pending = counts.Pending,
                approved = counts.Approved,
                rejected = counts.Rejected,
                total = counts.Pending + counts.Approved + counts.Rejected
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personnel request counts");
            return StatusCode(500, CreateErrorResponse("Sayılar yüklenirken hata oluştu", ex));
        }
    }
}
