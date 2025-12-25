using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Assignment;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/assignments")]
[Authorize]
public class AssignmentsApiController : BaseApiController
{
    private readonly IAssignmentService _assignmentService;
    private readonly IFieldWorkerService _fieldWorkerService;
    private readonly IQRCodeService _qrCodeService;
    private readonly ILogger<AssignmentsApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public AssignmentsApiController(
        IAssignmentService assignmentService,
        IFieldWorkerService fieldWorkerService,
        IQRCodeService qrCodeService,
        ILogger<AssignmentsApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _assignmentService = assignmentService;
        _fieldWorkerService = fieldWorkerService;
        _qrCodeService = qrCodeService;
        _logger = logger;
        _localizationService = localizationService;
    }

    #region TEMEL CRUD

    /// <summary>
    /// Get all assignments - Only Admin and TeamLeader can access
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetAll([FromQuery] int? projectId = null, [FromQuery] int? branchId = null)
    {
        try
        {
            IEnumerable<AssignmentDto> assignments;

            if (projectId.HasValue && projectId != 0)
            {
                assignments = await _assignmentService.GetByProjectIdAsync(projectId.Value);
            }
            else if (branchId.HasValue && branchId != 0)
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.LoadError"), ex));
        }
    }

    /// <summary>
    /// Get assignment by ID - Users can only access their own assignments (except Admin/TeamLeader)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var assignment = await _assignmentService.GetByIdAsync(id);
            if (assignment == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.NotFound")));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.LoadError"), ex));
        }
    }

    /// <summary>
    /// Get assignment detail with evaluation info and history
    /// </summary>
    [HttpGet("{id}/detail")]
    public async Task<IActionResult> GetDetail(int id)
    {
        try
        {
            var assignment = await _assignmentService.GetDetailByIdAsync(id);
            if (assignment == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.NotFound")));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.DetailLoadError"), ex));
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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.NotFound")));
            }

            return Ok(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignment by link {UniqueLink}", uniqueLink);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.LoadError"), ex));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.CreateError"), ex));
        }
    }

    [HttpPost("bulk")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> CreateBulk([FromBody] BulkAssignmentDto dto)
    {
        try
        {
            var assignments = await _assignmentService.CreateBulkAsync(dto);
            var successMsg = string.Format(await _localizationService.GetResourceAsync("Api.Assignment.BulkCreateSuccess"), assignments.Count());
            return Ok(new {
                message = successMsg,
                assignments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk assignments");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.BulkCreateError"), ex));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAssignmentDto dto)
    {
        try
        {
            await _assignmentService.UpdateAsync(id, dto);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.UpdateError"), ex));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _assignmentService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.NotFound")));
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.DeleteError"), ex));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.FilterError"), ex));
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
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Auth.UserNotFound")));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.LoadError"), ex));
        }
    }

    /// <summary>
    /// Get assignments by project
    /// </summary>
    [HttpGet("by-project/{projectId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetByProject(int projectId)
    {
        try
        {
            var assignments = await _assignmentService.GetByProjectIdAsync(projectId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignments for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.ProjectLoadError"), ex));
        }
    }

    /// <summary>
    /// Get assignments by branch
    /// </summary>
    [HttpGet("by-branch/{branchId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetByBranch(int branchId)
    {
        try
        {
            var assignments = await _assignmentService.GetByBranchIdAsync(branchId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignments for branch {BranchId}", branchId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.BranchLoadError"), ex));
        }
    }

    /// <summary>
    /// Get assignments by field worker
    /// </summary>
    [HttpGet("by-fieldworker/{fieldWorkerId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetByFieldWorker(int fieldWorkerId)
    {
        try
        {
            var assignments = await _assignmentService.GetByFieldWorkerIdAsync(fieldWorkerId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignments for field worker {FieldWorkerId}", fieldWorkerId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.FieldWorkerLoadError"), ex));
        }
    }

    #endregion

    #region DURUM YÖNETİMİ

    /// <summary>
    /// Complete an assignment
    /// </summary>
    [HttpPost("{id}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        try
        {
            var assignment = await _assignmentService.GetByIdAsync(id);
            if (assignment == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.NotFound")));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.CompleteError"), ex));
        }
    }

    /// <summary>
    /// Cancel an assignment
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelAssignmentDto dto)
    {
        try
        {
            var cancelled = await _assignmentService.CancelAssignmentAsync(id, dto.Reason);
            return Ok(cancelled);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.CancelError"), ex));
        }
    }

    /// <summary>
    /// Reassign an assignment to a different user
    /// </summary>
    [HttpPost("{id}/reassign")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> Reassign(int id, [FromBody] ReassignAssignmentDto dto)
    {
        try
        {
            var reassigned = await _assignmentService.ReassignAsync(id, dto);
            return Ok(reassigned);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reassigning assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.ReassignError"), ex));
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
            var successMsg = string.Format(await _localizationService.GetResourceAsync("Api.Assignment.BulkCreateSuccess"), assignments.Count());
            return Ok(new {
                message = successMsg,
                assignments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignments for project branches");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.ProjectCreateError"), ex));
        }
    }

    /// <summary>
    /// Delete all assignments for a project
    /// </summary>
    [HttpDelete("by-project/{projectId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteByProject(int projectId)
    {
        try
        {
            var count = await _assignmentService.DeleteByProjectIdAsync(projectId);
            var successMsg = string.Format(await _localizationService.GetResourceAsync("Api.Assignment.BulkDeleteSuccess"), count);
            return Ok(new { message = successMsg });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assignments for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.ProjectDeleteError"), ex));
        }
    }

    #endregion

    #region İSTATİSTİKLER

    /// <summary>
    /// Get assignment summary statistics
    /// </summary>
    [HttpGet("summary")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetSummary([FromQuery] int? projectId = null)
    {
        try
        {
            var summary = await _assignmentService.GetSummaryAsync(projectId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignment summary");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.SummaryLoadError"), ex));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.ProjectSummaryError"), ex));
        }
    }

    /// <summary>
    /// Get branch assignment summaries for a project
    /// </summary>
    [HttpGet("branch-summaries/{projectId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetBranchSummaries(int projectId)
    {
        try
        {
            var summaries = await _assignmentService.GetBranchSummariesAsync(projectId);
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading branch summaries for project {ProjectId}", projectId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.BranchSummaryError"), ex));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.ExpiredLoadError"), ex));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.UpcomingLoadError"), ex));
        }
    }

    #endregion

    #region QR CODE

    [HttpGet("{id}/qr-code")]
    [AllowAnonymous]
    public async Task<IActionResult> GetQRCode(int id)
    {
        try
        {
            var assignment = await _assignmentService.GetByIdAsync(id);
            if (assignment == null)
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.NotFound")));

            var baseUrl = $"{Request.Scheme}://{Request.Host}";

            var qrBytes = _qrCodeService.GenerateAssignmentQRCode(
                assignment.UniqueLink,
                baseUrl);

            return File(qrBytes, "image/png", $"assignment-{id}-qr.png");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code for assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.QRCodeError"), ex));
        }
    }

    [HttpGet("{id}/qr-code/base64")]
    [AllowAnonymous]
    public async Task<ActionResult<object>> GetQRCodeBase64(int id)
    {
        try
        {
            var assignment = await _assignmentService.GetByIdAsync(id);
            if (assignment == null)
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.NotFound")));

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var qrBase64 = _qrCodeService.GenerateQRCodeBase64(
                $"{baseUrl}/form/{assignment.UniqueLink}");

            return Ok(new { qrCode = $"data:image/png;base64,{qrBase64}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating QR code base64 for assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.QRCodeError"), ex));
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

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
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
