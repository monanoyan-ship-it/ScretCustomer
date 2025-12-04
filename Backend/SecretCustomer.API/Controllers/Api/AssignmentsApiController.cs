using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Assignment;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/assignments")]
[Authorize]
public class AssignmentsApiController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;
    private readonly IQRCodeService _qrCodeService;
    private readonly ILogger<AssignmentsApiController> _logger;

    public AssignmentsApiController(
        IAssignmentService assignmentService,
        IQRCodeService qrCodeService,
        ILogger<AssignmentsApiController> logger)
    {
        _assignmentService = assignmentService;
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? projectId = null, [FromQuery] Guid? branchId = null)
    {
        try
        {
            IEnumerable<AssignmentDto> assignments;

            if (projectId.HasValue && projectId != Guid.Empty)
            {
                assignments = await _assignmentService.GetByProjectIdAsync(projectId.Value);
            }
            else if (branchId.HasValue)
            {
                assignments = await _assignmentService.GetByBranchIdAsync(branchId.Value);
            }
            else
            {
                // Return empty for now - you might want to add GetAllAsync to service
                return Ok(new List<AssignmentDto>());
            }

            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignments");
            return StatusCode(500, new { message = "Atamalar yüklenirken bir hata oluştu." });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var assignment = await _assignmentService.GetByIdAsync(id);
            if (assignment == null)
            {
                return NotFound(new { message = "Atama bulunamadı." });
            }

            return Ok(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignment {Id}", id);
            return StatusCode(500, new { message = "Atama yüklenirken bir hata oluştu." });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _assignmentService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Atama bulunamadı." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assignment {Id}", id);
            return StatusCode(500, new { message = "Atama silinirken bir hata oluştu." });
        }
    }

    [HttpGet("{id}/qr-code")]
    [AllowAnonymous] // QR kod herkes tarafından erişilebilir olmalı
    public async Task<IActionResult> GetQRCode(Guid id)
    {
        try
        {
            var assignment = await _assignmentService.GetByIdAsync(id);
            if (assignment == null)
                return NotFound(new { message = "Atama bulunamadı." });

            // Base URL oluştur (request'ten al)
            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var qrBytes = _qrCodeService.GenerateAssignmentQRCode(
                assignment.UniqueLink,
                baseUrl);

            return File(qrBytes, "image/png", $"assignment-{id}-qr.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for assignment {Id}", id);
            return StatusCode(500, new { message = "QR kod oluşturulurken bir hata oluştu." });
        }
    }

    [HttpGet("{id}/qr-code/base64")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetQRCodeBase64(Guid id)
    {
        try
        {
            var assignment = await _assignmentService.GetByIdAsync(id);
            if (assignment == null)
                return NotFound(new { message = "Atama bulunamadı." });

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var qrBase64 = _qrCodeService.GenerateQRCodeBase64(
                $"{baseUrl}/form/{assignment.UniqueLink}");

            return Ok(new { qrCode = $"data:image/png;base64,{qrBase64}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code base64 for assignment {Id}", id);
            return StatusCode(500, new { message = "QR kod oluşturulurken bir hata oluştu." });
        }
    }
}
