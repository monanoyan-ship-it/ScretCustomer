using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.SupportRequest;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

/// <summary>
/// Destek Talepleri API Controller
/// Kullanıcıların hata/talep bildirmesi ve admin'in cevap vermesi
/// </summary>
[Route("api/support-requests")]
[ApiController]
[Authorize]
public class SupportRequestApiController : BaseApiController
{
    private readonly ISupportRequestService _supportRequestService;
    private readonly IAuditLogService _auditLogService;

    public SupportRequestApiController(
        ISupportRequestService supportRequestService,
        IAuditLogService auditLogService,
        IConfiguration configuration) : base(configuration)
    {
        _supportRequestService = supportRequestService;
        _auditLogService = auditLogService;
    }

    private int? GetCurrentUserId()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdString, out var userId) ? userId : null;
    }

    private int? GetCurrentCustomerPersonnelId()
    {
        var personnelIdString = User.FindFirst("CustomerPersonnelId")?.Value;
        return int.TryParse(personnelIdString, out var personnelId) ? personnelId : null;
    }

    private bool IsAdmin()
    {
        return User.IsInRole("Admin");
    }

    #region Admin Endpoints

    /// <summary>
    /// Tüm destek taleplerini filtreli getirir (Admin)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll([FromQuery] SupportRequestFilterDto filter)
    {
        try
        {
            var (items, totalCount) = await _supportRequestService.GetAllAsync(filter);
            return Ok(new { items, totalCount, page = filter.Page, pageSize = filter.PageSize });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting support requests", "SupportRequest", ex);
            return StatusCode(500, CreateErrorResponse("Destek talepleri yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Destek talebi detayını getirir (Admin)
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _supportRequestService.GetByIdAsync(id);
            if (result == null)
                return NotFound(CreateErrorResponse("Destek talebi bulunamadı."));

            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting support request {id}", "SupportRequest", ex);
            return StatusCode(500, CreateErrorResponse("Destek talebi yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Destek talebine cevap verir (Admin)
    /// </summary>
    [HttpPost("{id}/respond")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Respond(int id, [FromBody] RespondSupportRequestDto dto)
    {
        try
        {
            dto.Id = id;
            var adminUserId = GetCurrentUserId();
            if (!adminUserId.HasValue)
                return Unauthorized(CreateErrorResponse("Kullanıcı bulunamadı."));

            var result = await _supportRequestService.RespondAsync(dto, adminUserId.Value);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error responding to support request {id}", "SupportRequest", ex);
            return StatusCode(500, CreateErrorResponse("Destek talebi cevaplanırken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Destek talebi durumunu günceller (Admin)
    /// </summary>
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateSupportRequestStatusDto dto)
    {
        try
        {
            dto.Id = id;
            var result = await _supportRequestService.UpdateStatusAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating support request status {id}", "SupportRequest", ex);
            return StatusCode(500, CreateErrorResponse("Destek talebi durumu güncellenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Destek talebini siler (Admin)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _supportRequestService.DeleteAsync(id);
            return Ok(new { message = "Destek talebi başarıyla silindi." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error deleting support request {id}", "SupportRequest", ex);
            return StatusCode(500, CreateErrorResponse("Destek talebi silinirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Durum sayılarını getirir (Admin)
    /// </summary>
    [HttpGet("counts")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetStatusCounts()
    {
        try
        {
            var (pending, inProgress, resolved, closed) = await _supportRequestService.GetStatusCountsAsync();
            return Ok(new { pending, inProgress, resolved, closed, total = pending + inProgress + resolved + closed });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting support request status counts", "SupportRequest", ex);
            return StatusCode(500, CreateErrorResponse("Durum sayıları yüklenirken hata oluştu.", ex));
        }
    }

    #endregion

    #region User Endpoints

    /// <summary>
    /// Yeni destek talebi oluşturur (Tüm kullanıcılar)
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSupportRequestDto dto)
    {
        try
        {
            var userId = GetCurrentUserId();
            var customerPersonnelId = GetCurrentCustomerPersonnelId();

            // En az bir ID olmalı
            if (!userId.HasValue && !customerPersonnelId.HasValue)
                return Unauthorized(CreateErrorResponse("Kullanıcı tanımlanamadı."));

            var result = await _supportRequestService.CreateAsync(dto, userId, customerPersonnelId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error creating support request", "SupportRequest", ex);
            return StatusCode(500, CreateErrorResponse("Destek talebi oluşturulurken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Kullanıcının kendi taleplerini getirir
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMyRequests()
    {
        try
        {
            var userId = GetCurrentUserId();
            var customerPersonnelId = GetCurrentCustomerPersonnelId();

            var result = await _supportRequestService.GetByUserAsync(userId, customerPersonnelId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting my support requests", "SupportRequest", ex);
            return StatusCode(500, CreateErrorResponse("Destek talepleri yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Kullanıcının talep özetini getirir
    /// </summary>
    [HttpGet("my/summary")]
    public async Task<IActionResult> GetMySummary()
    {
        try
        {
            var userId = GetCurrentUserId();
            var customerPersonnelId = GetCurrentCustomerPersonnelId();

            var result = await _supportRequestService.GetMySummaryAsync(userId, customerPersonnelId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting my support request summary", "SupportRequest", ex);
            return StatusCode(500, CreateErrorResponse("Talep özeti yüklenirken hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Kullanıcı kendi talebinin detayını görür
    /// </summary>
    [HttpGet("my/{id}")]
    public async Task<IActionResult> GetMyRequestById(int id)
    {
        try
        {
            var userId = GetCurrentUserId();
            var customerPersonnelId = GetCurrentCustomerPersonnelId();

            var result = await _supportRequestService.GetByIdAsync(id);
            if (result == null)
                return NotFound(CreateErrorResponse("Destek talebi bulunamadı."));

            // Sadece kendi talebini görebilir
            if ((userId.HasValue && result.UserId == userId) ||
                (customerPersonnelId.HasValue && result.CustomerPersonnelId == customerPersonnelId))
            {
                // Okundu olarak işaretle
                await _supportRequestService.MarkAsReadAsync(id, userId, customerPersonnelId);
                return Ok(result);
            }

            return Forbid();
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting my support request {id}", "SupportRequest", ex);
            return StatusCode(500, CreateErrorResponse("Destek talebi yüklenirken hata oluştu.", ex));
        }
    }

    #endregion
}
