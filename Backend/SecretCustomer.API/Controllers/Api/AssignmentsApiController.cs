using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Assignment;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/assignments")]
[Authorize]
public class AssignmentsApiController : BaseApiController
{
    private readonly IAssignmentService _assignmentService;
    private readonly IProjectService _projectService;
    private readonly IQRCodeService _qrCodeService;
    private readonly ILogger<AssignmentsApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public AssignmentsApiController(
        IAssignmentService assignmentService,
        IProjectService projectService,
        IQRCodeService qrCodeService,
        ILogger<AssignmentsApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _assignmentService = assignmentService;
        _projectService = projectService;
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
    public async Task<IActionResult> GetAll([FromQuery] int? projectId = null)
    {
        try
        {
            IEnumerable<AssignmentDto> assignments;

            if (projectId.HasValue && projectId != 0)
            {
                assignments = await _assignmentService.GetByProjectIdAsync(projectId.Value);
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
            // Aktif proje kontrolü
            var project = await _projectService.GetByIdAsync(dto.ProjectId);
            if (project == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
            }

            if (project.Status != ProjectStatuses.Active.SystemName)
            {
                // ForceActivateProject true ise projeyi aktif et
                if (dto.ForceActivateProject)
                {
                    await _projectService.StartProjectAsync(dto.ProjectId);
                }
                else
                {
                    // Özel hata kodu ile dön - frontend bunu yakalayıp onay isteyecek
                    return BadRequest(new {
                        message = await _localizationService.GetResourceAsync("Api.Assignment.ProjectNotActive"),
                        errorCode = "PROJECT_NOT_ACTIVE",
                        projectId = dto.ProjectId
                    });
                }
            }

            var assignment = await _assignmentService.CreateAsync(dto);
            return Ok(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignment");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.CreateError"), ex));
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
    /// Get my assignments - for field workers, evaluators, and customer personnel
    /// </summary>
    [HttpGet("my-assignments")]
    public async Task<IActionResult> GetMyAssignments()
    {
        try
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(idClaim) || !int.TryParse(idClaim, out var id))
            {
                _logger.LogWarning("GetMyAssignments: ID claim not found or invalid. Claim value: {Claim}", idClaim);
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Auth.UserNotFound")));
            }

            var userType = User.FindFirstValue("UserType") ?? "User";

            IEnumerable<Core.DTOs.Assignment.AssignmentDto> assignments;

            if (userType == "CustomerPersonnel")
            {
                // CustomerPersonnel için: NameIdentifier = CustomerPersonnelId
                _logger.LogInformation("GetMyAssignments: Fetching assignments for customerPersonnelId={Id}", id);
                assignments = await _assignmentService.GetByCustomerPersonnelIdAsync(id);
            }
            else
            {
                // Normal kullanıcılar için: NameIdentifier = UserId
                _logger.LogInformation("GetMyAssignments: Fetching assignments for userId={Id}", id);
                assignments = await _assignmentService.GetByUserIdAsync(id);
            }

            _logger.LogInformation("GetMyAssignments: Found {Count} assignments for id={Id}, userType={UserType}",
                assignments?.Count() ?? 0, id, userType);

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
    /// Get assignments by user
    /// </summary>
    [HttpGet("by-user/{userId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        try
        {
            var assignments = await _assignmentService.GetByUserIdAsync(userId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignments for user {UserId}", userId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.UserLoadError"), ex));
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
    /// Reopen a completed assignment
    /// </summary>
    [HttpPost("{id}/reopen")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> Reopen(int id)
    {
        try
        {
            var reopened = await _assignmentService.ReopenAssignmentAsync(id);
            return Ok(reopened);
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
            _logger.LogError(ex, "Error reopening assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse("Atama yeniden açılırken bir hata oluştu.", ex));
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

    /// <summary>
    /// Update due date of an assignment
    /// </summary>
    [HttpPost("{id}/update-due-date")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> UpdateDueDate(int id, [FromBody] UpdateDueDateDto dto)
    {
        try
        {
            var updated = await _assignmentService.UpdateDueDateAsync(id, dto.NewDueDate);
            return Ok(updated);
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
            _logger.LogError(ex, "Error updating due date for assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse("Tarih güncellenirken bir hata oluştu.", ex));
        }
    }

    #endregion

    #region TOPLU İŞLEMLER

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

    #region DÖNEMLER (PERIODS)

    /// <summary>
    /// Get periods for an assignment
    /// </summary>
    [HttpGet("{id}/periods")]
    [Authorize(Roles = "Admin,TeamLeader,QualitySpecialist")]
    public async Task<IActionResult> GetPeriods(int id)
    {
        try
        {
            var periods = await _assignmentService.GetPeriodsAsync(id);
            return Ok(periods);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading periods for assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse("Dönemler yüklenirken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Create a new period for an assignment
    /// </summary>
    [HttpPost("{id}/periods")]
    [Authorize(Roles = "Admin,TeamLeader,QualitySpecialist")]
    public async Task<IActionResult> CreatePeriod(int id, [FromBody] Core.DTOs.AssignmentPeriod.CreateAssignmentPeriodDto dto)
    {
        try
        {
            dto.AssignmentId = id;
            var period = await _assignmentService.CreatePeriodAsync(dto);
            return Ok(period);
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
            _logger.LogError(ex, "Error creating period for assignment {Id}", id);
            return StatusCode(500, CreateErrorResponse("Dönem oluşturulurken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Update a period
    /// </summary>
    [HttpPut("{id}/periods/{periodId}")]
    [Authorize(Roles = "Admin,TeamLeader,QualitySpecialist")]
    public async Task<IActionResult> UpdatePeriod(int id, int periodId, [FromBody] Core.DTOs.AssignmentPeriod.UpdateAssignmentPeriodDto dto)
    {
        try
        {
            dto.Id = periodId;
            var period = await _assignmentService.UpdatePeriodAsync(dto);
            return Ok(period);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating period {PeriodId}", periodId);
            return StatusCode(500, CreateErrorResponse("Dönem güncellenirken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Close a period
    /// </summary>
    [HttpPost("{id}/periods/{periodId}/close")]
    [Authorize(Roles = "Admin,TeamLeader,QualitySpecialist")]
    public async Task<IActionResult> ClosePeriod(int id, int periodId)
    {
        try
        {
            var period = await _assignmentService.ClosePeriodAsync(periodId);
            return Ok(period);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing period {PeriodId}", periodId);
            return StatusCode(500, CreateErrorResponse("Dönem kapatılırken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Reopen a closed period
    /// </summary>
    [HttpPost("{id}/periods/{periodId}/reopen")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> ReopenPeriod(int id, int periodId)
    {
        try
        {
            var period = await _assignmentService.ReopenPeriodAsync(periodId);
            return Ok(period);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reopening period {PeriodId}", periodId);
            return StatusCode(500, CreateErrorResponse("Dönem yeniden açılırken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Delete a period
    /// </summary>
    [HttpDelete("{id}/periods/{periodId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeletePeriod(int id, int periodId)
    {
        try
        {
            await _assignmentService.DeletePeriodAsync(periodId);
            return Ok(new { message = "Dönem başarıyla silindi." });
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
            _logger.LogError(ex, "Error deleting period {PeriodId}", periodId);
            return StatusCode(500, CreateErrorResponse("Dönem silinirken bir hata oluştu.", ex));
        }
    }

    #endregion

    #region HELPER METHODS

    /// <summary>
    /// Check if current user is authorized to access the assignment
    /// </summary>
    private Task<bool> IsAuthorizedForAssignment(AssignmentDto assignment)
    {
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
        {
            return Task.FromResult(false);
        }

        // Admin ve TeamLeader her şeyi görebilir
        if (userRole == "Admin" || userRole == "TeamLeader")
        {
            return Task.FromResult(true);
        }

        // Kullanıcı sadece kendine atanmış assignment'ları görebilir
        if (assignment.AssignedUserId != userId)
        {
            _logger.LogWarning("User {UserId} attempted to access unauthorized assignment {AssignmentId}", userId, assignment.Id);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    #endregion
}
