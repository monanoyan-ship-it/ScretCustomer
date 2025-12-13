using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Assignment;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/assignments")]
[Authorize]
public class AssignmentsApiController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;
    private readonly IFieldWorkerService _fieldWorkerService;
    private readonly IQRCodeService _qrCodeService;
    private readonly ILogger<AssignmentsApiController> _logger;

    public AssignmentsApiController(
        IAssignmentService assignmentService,
        IFieldWorkerService fieldWorkerService,
        IQRCodeService qrCodeService,
        ILogger<AssignmentsApiController> logger)
    {
        _assignmentService = assignmentService;
        _fieldWorkerService = fieldWorkerService;
        _qrCodeService = qrCodeService;
        _logger = logger;
    }

    #region TEMEL CRUD

    /// <summary>
    /// Get all assignments - Only Admin and TeamLeader can access
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetAll([FromQuery] Guid? projectId = null, [FromQuery] Guid? branchId = null)
    {
        try
        {
            IEnumerable<AssignmentDto> assignments;

            if (projectId.HasValue && projectId != Guid.Empty)
            {
                assignments = await _assignmentService.GetByProjectIdAsync(projectId.Value);
            }
            else if (branchId.HasValue && branchId != Guid.Empty)
            {
                assignments = await _assignmentService.GetByBranchIdAsync(branchId.Value);
            }
            else
            {
                assignments = await _assignmentService.GetAllAsync();
            }

            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignments");
            return StatusCode(500, new { message = "Atamalar yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Get assignment by ID - Users can only access their own assignments (except Admin/TeamLeader)
    /// </summary>
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

            if (!await IsAuthorizedForAssignment(assignment))
            {
                return Forbid();
            }

            return Ok(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignment {Id}", id);
            return StatusCode(500, new { message = "Atama yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Get assignment detail with evaluation info and history
    /// </summary>
    [HttpGet("{id}/detail")]
    public async Task<IActionResult> GetDetail(Guid id)
    {
        try
        {
            var assignment = await _assignmentService.GetDetailByIdAsync(id);
            if (assignment == null)
            {
                return NotFound(new { message = "Atama bulunamadı." });
            }

            if (!await IsAuthorizedForAssignment(assignment))
            {
                return Forbid();
            }

            return Ok(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignment detail {Id}", id);
            return StatusCode(500, new { message = "Atama detayı yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Get assignment by unique link - for public form access
    /// </summary>
    [HttpGet("by-link/{uniqueLink}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByUniqueLink(string uniqueLink)
    {
        try
        {
            var assignment = await _assignmentService.GetByUniqueLinkAsync(uniqueLink);
            if (assignment == null)
            {
                return NotFound(new { message = "Atama bulunamadı." });
            }

            return Ok(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignment by link {UniqueLink}", uniqueLink);
            return StatusCode(500, new { message = "Atama yüklenirken bir hata oluştu." });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> Create([FromBody] CreateAssignmentDto dto)
    {
        try
        {
            var assignment = await _assignmentService.CreateAsync(dto);
            return Ok(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignment");
            return StatusCode(500, new { message = "Atama oluşturulurken bir hata oluştu: " + ex.Message });
        }
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> CreateBulk([FromBody] BulkAssignmentDto dto)
    {
        try
        {
            var assignments = await _assignmentService.CreateBulkAsync(dto);
            return Ok(new {
                message = $"{assignments.Count()} atama başarıyla oluşturuldu.",
                assignments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk assignments");
            return StatusCode(500, new { message = "Toplu atama oluşturulurken bir hata oluştu: " + ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAssignmentDto dto)
    {
        try
        {
            await _assignmentService.UpdateAsync(id, dto);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assignment {Id}", id);
            return StatusCode(500, new { message = "Atama güncellenirken bir hata oluştu: " + ex.Message });
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

    #endregion

    #region FİLTRELEME

    /// <summary>
    /// Get filtered assignments
    /// </summary>
    [HttpPost("filter")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetFiltered([FromBody] AssignmentFilterDto filter)
    {
        try
        {
            var assignments = await _assignmentService.GetFilteredAsync(filter);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error filtering assignments");
            return StatusCode(500, new { message = "Atamalar filtrelenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Get my assignments - for field workers and evaluators
    /// </summary>
    [HttpGet("my-assignments")]
    public async Task<IActionResult> GetMyAssignments()
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Kullanıcı bilgisi bulunamadı." });
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            IEnumerable<AssignmentDto> assignments;

            if (userRole == "FieldWorker")
            {
                // FieldWorker için kendi saha çalışanı ID'sini bul
                var fieldWorker = await _fieldWorkerService.GetByUserIdAsync(userId);
                if (fieldWorker != null)
                {
                    assignments = await _assignmentService.GetByFieldWorkerIdAsync(fieldWorker.Id);
                }
                else
                {
                    assignments = Enumerable.Empty<AssignmentDto>();
                }
            }
            else
            {
                // User ID ile ara
                assignments = await _assignmentService.GetByUserIdAsync(userId);
            }

            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user assignments");
            return StatusCode(500, new { message = "Atamalar yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Get assignments by project
    /// </summary>
    [HttpGet("by-project/{projectId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetByProject(Guid projectId)
    {
        try
        {
            var assignments = await _assignmentService.GetByProjectIdAsync(projectId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignments for project {ProjectId}", projectId);
            return StatusCode(500, new { message = "Proje atamaları yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Get assignments by branch
    /// </summary>
    [HttpGet("by-branch/{branchId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetByBranch(Guid branchId)
    {
        try
        {
            var assignments = await _assignmentService.GetByBranchIdAsync(branchId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignments for branch {BranchId}", branchId);
            return StatusCode(500, new { message = "Şube atamaları yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Get assignments by field worker
    /// </summary>
    [HttpGet("by-fieldworker/{fieldWorkerId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetByFieldWorker(Guid fieldWorkerId)
    {
        try
        {
            var assignments = await _assignmentService.GetByFieldWorkerIdAsync(fieldWorkerId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignments for field worker {FieldWorkerId}", fieldWorkerId);
            return StatusCode(500, new { message = "Saha çalışanı atamaları yüklenirken bir hata oluştu." });
        }
    }

    #endregion

    #region DURUM YÖNETİMİ

    /// <summary>
    /// Complete an assignment
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        try
        {
            var assignment = await _assignmentService.GetByIdAsync(id);
            if (assignment == null)
            {
                return NotFound(new { message = "Atama bulunamadı." });
            }

            if (!await IsAuthorizedForAssignment(assignment))
            {
                return Forbid();
            }

            var completed = await _assignmentService.CompleteAssignmentAsync(id);
            return Ok(completed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing assignment {Id}", id);
            return StatusCode(500, new { message = "Atama tamamlanırken bir hata oluştu: " + ex.Message });
        }
    }

    /// <summary>
    /// Cancel an assignment
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> Cancel(Guid id, [FromBody] CancelAssignmentDto dto)
    {
        try
        {
            var cancelled = await _assignmentService.CancelAssignmentAsync(id, dto.Reason);
            return Ok(cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling assignment {Id}", id);
            return StatusCode(500, new { message = "Atama iptal edilirken bir hata oluştu: " + ex.Message });
        }
    }

    /// <summary>
    /// Reassign an assignment to a different user
    /// </summary>
    [HttpPost("{id}/reassign")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> Reassign(Guid id, [FromBody] ReassignAssignmentDto dto)
    {
        try
        {
            var reassigned = await _assignmentService.ReassignAsync(id, dto);
            return Ok(reassigned);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reassigning assignment {Id}", id);
            return StatusCode(500, new { message = "Atama yeniden atanırken bir hata oluştu: " + ex.Message });
        }
    }

    #endregion

    #region TOPLU İŞLEMLER

    /// <summary>
    /// Create assignments for all branches in a project
    /// </summary>
    [HttpPost("project-bulk")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> CreateForProjectBranches([FromBody] BulkProjectAssignmentDto dto)
    {
        try
        {
            var assignments = await _assignmentService.CreateForProjectBranchesAsync(dto);
            return Ok(new {
                message = $"{assignments.Count()} atama başarıyla oluşturuldu.",
                assignments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignments for project branches");
            return StatusCode(500, new { message = "Proje atamaları oluşturulurken bir hata oluştu: " + ex.Message });
        }
    }

    /// <summary>
    /// Delete all assignments for a project
    /// </summary>
    [HttpDelete("by-project/{projectId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteByProject(Guid projectId)
    {
        try
        {
            var count = await _assignmentService.DeleteByProjectIdAsync(projectId);
            return Ok(new { message = $"{count} atama başarıyla silindi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assignments for project {ProjectId}", projectId);
            return StatusCode(500, new { message = "Proje atamaları silinirken bir hata oluştu." });
        }
    }

    #endregion

    #region İSTATİSTİKLER

    /// <summary>
    /// Get assignment summary statistics
    /// </summary>
    [HttpGet("summary")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetSummary([FromQuery] Guid? projectId = null)
    {
        try
        {
            var summary = await _assignmentService.GetSummaryAsync(projectId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignment summary");
            return StatusCode(500, new { message = "Atama özeti yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Get project assignment summaries
    /// </summary>
    [HttpGet("project-summaries")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetProjectSummaries()
    {
        try
        {
            var summaries = await _assignmentService.GetProjectSummariesAsync();
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading project summaries");
            return StatusCode(500, new { message = "Proje özetleri yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Get branch assignment summaries for a project
    /// </summary>
    [HttpGet("branch-summaries/{projectId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetBranchSummaries(Guid projectId)
    {
        try
        {
            var summaries = await _assignmentService.GetBranchSummariesAsync(projectId);
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading branch summaries for project {ProjectId}", projectId);
            return StatusCode(500, new { message = "Şube özetleri yüklenirken bir hata oluştu." });
        }
    }

    #endregion

    #region SÜRESİ DOLANLAR

    /// <summary>
    /// Get expired assignments
    /// </summary>
    [HttpGet("expired")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetExpired()
    {
        try
        {
            var expired = await _assignmentService.GetExpiredAsync();
            return Ok(expired);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading expired assignments");
            return StatusCode(500, new { message = "Süresi dolan atamalar yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Get upcoming due assignments
    /// </summary>
    [HttpGet("upcoming-due")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetUpcomingDue([FromQuery] int daysAhead = 3)
    {
        try
        {
            var upcoming = await _assignmentService.GetUpcomingDueAsync(daysAhead);
            return Ok(upcoming);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading upcoming due assignments");
            return StatusCode(500, new { message = "Yaklaşan atamalar yüklenirken bir hata oluştu." });
        }
    }

    #endregion

    #region QR CODE

    [HttpGet("{id}/qr-code")]
    [AllowAnonymous]
    public async Task<IActionResult> GetQRCode(Guid id)
    {
        try
        {
            var assignment = await _assignmentService.GetByIdAsync(id);
            if (assignment == null)
                return NotFound(new { message = "Atama bulunamadı." });

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

    #endregion

    #region HELPER METHODS

    /// <summary>
    /// Check if current user is authorized to access the assignment
    /// </summary>
    private async Task<bool> IsAuthorizedForAssignment(AssignmentDto assignment)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            return false;
        }

        // Admin ve TeamLeader her şeyi görebilir
        if (userRole == "Admin" || userRole == "TeamLeader")
        {
            return true;
        }

        // FieldWorker ise sadece kendi assignment'ını görebilir
        if (userRole == "FieldWorker")
        {
            var fieldWorker = await _fieldWorkerService.GetByUserIdAsync(userId);
            if (fieldWorker == null || assignment.AssignedFieldWorkerId != fieldWorker.Id)
            {
                _logger.LogWarning("FieldWorker {UserId} attempted to access unauthorized assignment {AssignmentId}", userId, assignment.Id);
                return false;
            }
            return true;
        }

        // Diğer roller için sadece kendine atanmış assignment'ları görebilir
        if (assignment.AssignedUserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to access unauthorized assignment {AssignmentId}", userId, assignment.Id);
            return false;
        }

        return true;
    }

    #endregion
}
