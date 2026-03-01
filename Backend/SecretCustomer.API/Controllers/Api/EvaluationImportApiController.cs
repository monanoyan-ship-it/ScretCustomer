using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.EvaluationImport;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/evaluation-import")]
[ApiController]
[Authorize(Roles = "Admin")]
public class EvaluationImportApiController : BaseApiController
{
    private readonly IEvaluationImportService _importService;

    public EvaluationImportApiController(
        IEvaluationImportService importService,
        IConfiguration configuration) : base(configuration)
    {
        _importService = importService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(userIdClaim, out var userId) ? userId : 0;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] int? customerId = null)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(CreateErrorResponse("Dosya seçilmedi."));

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);

            var result = await _importService.UploadAndProcessAsync(
                ms.ToArray(), file.FileName, GetCurrentUserId(), customerId);

            return Ok(result);
        }
        catch (InvalidOperationException ex) when (ex.Message == "CUSTOMER_REQUIRED")
        {
            return BadRequest(new { code = "CUSTOMER_REQUIRED", message = "Excel'deki firma adı sistemde bulunamadı. Lütfen müşteri seçin." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Excel dosyası işlenirken hata oluştu.", ex));
        }
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions()
    {
        try
        {
            var sessions = await _importService.GetSessionsAsync();
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Oturumlar yüklenirken hata oluştu.", ex));
        }
    }

    [HttpGet("sessions/{id}")]
    public async Task<IActionResult> GetSessionDetail(int id)
    {
        try
        {
            var detail = await _importService.GetSessionDetailAsync(id);
            return Ok(detail);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateErrorResponse("Oturum bulunamadı."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Oturum detayı yüklenirken hata oluştu.", ex));
        }
    }

    [HttpGet("sessions/{id}/unmatched")]
    public async Task<IActionResult> GetUnmatchedItems(int id, [FromQuery] int? itemType, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var items = await _importService.GetUnmatchedItemsAsync(id, itemType, page, pageSize);
            return Ok(items);
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Eşleşmeyen öğeler yüklenirken hata oluştu.", ex));
        }
    }

    [HttpPut("unmatched/{id}/resolve")]
    public async Task<IActionResult> ResolveUnmatchedItem(int id, [FromBody] ResolveUnmatchedItemDto dto)
    {
        try
        {
            var result = await _importService.ResolveUnmatchedItemAsync(id, dto, GetCurrentUserId());
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateErrorResponse("Eşleşmeyen öğe bulunamadı."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Eşleşme çözümlenirken hata oluştu.", ex));
        }
    }

    [HttpGet("sessions/{id}/pending-rows")]
    public async Task<IActionResult> GetPendingRows(int id, [FromQuery] int? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
    {
        try
        {
            var rows = await _importService.GetPendingRowsAsync(id, status, page, pageSize);
            return Ok(rows);
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Bekleyen satırlar yüklenirken hata oluştu.", ex));
        }
    }

    [HttpPost("sessions/{id}/import-resolved")]
    public async Task<IActionResult> ImportResolved(int id)
    {
        try
        {
            var result = await _importService.ImportResolvedRowsAsync(id, GetCurrentUserId());
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Import işlemi sırasında hata oluştu.", ex));
        }
    }

    [HttpDelete("sessions/{id}")]
    public async Task<IActionResult> DeleteSession(int id)
    {
        try
        {
            var result = await _importService.DeleteSessionAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateErrorResponse("Oturum bulunamadı."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Oturum silinirken hata oluştu.", ex));
        }
    }

    [HttpGet("search/personnel")]
    public async Task<IActionResult> SearchPersonnel([FromQuery] int customerId, [FromQuery] string q = "")
    {
        try
        {
            var results = await _importService.SearchCustomerPersonnelAsync(customerId, q);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Personel araması sırasında hata oluştu.", ex));
        }
    }

    [HttpGet("search/users")]
    public async Task<IActionResult> SearchUsers([FromQuery] string q = "")
    {
        try
        {
            var results = await _importService.SearchUsersAsync(q);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Kullanıcı araması sırasında hata oluştu.", ex));
        }
    }

    [HttpGet("search/projects")]
    public async Task<IActionResult> SearchProjects([FromQuery] int? customerId, [FromQuery] string q = "")
    {
        try
        {
            var results = await _importService.SearchProjectsAsync(customerId, q);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Proje araması sırasında hata oluştu.", ex));
        }
    }

    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomers()
    {
        try
        {
            var results = await _importService.GetCustomersAsync();
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Müşteri listesi yüklenirken hata oluştu.", ex));
        }
    }

    [HttpGet("checklists")]
    public async Task<IActionResult> GetChecklists()
    {
        try
        {
            var results = await _importService.GetChecklistsAsync();
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Kontrol listesi yüklenirken hata oluştu.", ex));
        }
    }
}
