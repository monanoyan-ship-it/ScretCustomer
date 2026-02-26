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
    private readonly IAuditLogService _auditLogService;
    private readonly ILocalizationService _localizationService;

    public AssignmentsApiController(
        IAssignmentService assignmentService,
        IProjectService projectService,
        IAuditLogService auditLogService,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _assignmentService = assignmentService;
        _projectService = projectService;
        _auditLogService = auditLogService;
        _localizationService = localizationService;
    }

    #region TEMEL CRUD

    /// <summary>
    /// Get all assignments - Only Admin and QualitySpecialist can access
    /// Optimized with projection (no Include)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetAll([FromQuery] AssignmentFilterDto filter)
    {
        try
        {
            var assignments = await _assignmentService.GetFilteredAsync(filter);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading assignments", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Assignment.LoadError"), ex));
        }
    }

    /// <summary>
    /// Get assignment by ID - Users can only access their own assignments (except Admin/QualitySpecialist)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var assignment = await _assignmentService.GetByIdAsync(id);
            if (assignment == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Assignment.NotFound")));
            }

            if (!await IsAuthorizedForAssignment(assignment))
            {
                return Forbid();
            }

            return Ok(assignment);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading assignment {id}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Assignment.LoadError"), ex));
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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Assignment.NotFound")));
            }

            if (!await IsAuthorizedForAssignment(assignment))
            {
                return Forbid();
            }

            return Ok(assignment);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading assignment detail {id}", "Assignments", ex);
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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Assignment.NotFound")));
            }

            return Ok(assignment);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading assignment by link {uniqueLink}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Assignment.LoadError"), ex));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
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
            await _auditLogService.LogErrorAsync($"Error creating assignment", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.CreateError"), ex));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAssignmentDto dto)
    {
        try
        {
            await _assignmentService.UpdateAsync(id, dto);
            return NoContent();
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating assignment {id}", "Assignments", ex);
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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Assignment.NotFound")));
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error deleting assignment {id}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Assignment.DeleteError"), ex));
        }
    }

    #endregion

    #region FİLTRELEME

    /// <summary>
    /// Get filtered assignments
    /// </summary>
    [HttpPost("filter")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetFiltered([FromBody] AssignmentFilterDto filter)
    {
        try
        {
            var assignments = await _assignmentService.GetFilteredAsync(filter);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error filtering assignments", "Assignments", ex);
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
                await _auditLogService.LogWarningAsync($"GetMyAssignments: ID claim not found or invalid. Claim value: {idClaim}", "Assignments");
                return Unauthorized(CreateErrorResponse(await _localizationService.GetResourceAsync("Auth.UserNotFound")));
            }

            var userType = User.FindFirstValue("UserType") ?? "User";

            IEnumerable<Core.DTOs.Assignment.AssignmentDto> assignments;

            if (userType == "CustomerPersonnel")
            {
                // CustomerPersonnel için: NameIdentifier = CustomerPersonnelId
                await _auditLogService.LogInfoAsync($"GetMyAssignments: Fetching assignments for customerPersonnelId={id}", "Assignments");
                assignments = await _assignmentService.GetByCustomerPersonnelIdAsync(id);
            }
            else
            {
                // Normal kullanıcılar için: NameIdentifier = UserId
                await _auditLogService.LogInfoAsync($"GetMyAssignments: Fetching assignments for userId={id}", "Assignments");
                assignments = await _assignmentService.GetByUserIdAsync(id);
            }

            await _auditLogService.LogInfoAsync($"GetMyAssignments: Found {assignments?.Count() ?? 0} assignments for id={id}, userType={userType}", "Assignments");

            return Ok(assignments);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading user assignments", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Assignment.LoadError"), ex));
        }
    }

    /// <summary>
    /// Get assignments by project
    /// </summary>
    [HttpGet("by-project/{projectId}")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetByProject(int projectId)
    {
        try
        {
            var assignments = await _assignmentService.GetByProjectIdAsync(projectId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading assignments for project {projectId}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.ProjectLoadError"), ex));
        }
    }

    /// <summary>
    /// Get assignments by user
    /// </summary>
    [HttpGet("by-user/{userId}")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetByUser(int userId)
    {
        try
        {
            var assignments = await _assignmentService.GetByUserIdAsync(userId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading assignments for user {userId}", "Assignments", ex);
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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Assignment.NotFound")));
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
            await _auditLogService.LogErrorAsync($"Error completing assignment {id}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.CompleteError"), ex));
        }
    }

    /// <summary>
    /// Cancel an assignment
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> Cancel(int id, [FromBody] CancelAssignmentDto dto)
    {
        try
        {
            var cancelled = await _assignmentService.CancelAssignmentAsync(id, dto.Reason);
            return Ok(cancelled);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error cancelling assignment {id}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.CancelError"), ex));
        }
    }

    /// <summary>
    /// Reopen a completed assignment
    /// </summary>
    [HttpPost("{id}/reopen")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
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
            await _auditLogService.LogErrorAsync($"Error reopening assignment {id}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse("Atama yeniden açılırken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Reassign an assignment to a different user
    /// </summary>
    [HttpPost("{id}/reassign")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> Reassign(int id, [FromBody] ReassignAssignmentDto dto)
    {
        try
        {
            var reassigned = await _assignmentService.ReassignAsync(id, dto);
            return Ok(reassigned);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error reassigning assignment {id}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.ReassignError"), ex));
        }
    }

    /// <summary>
    /// Update due date of an assignment
    /// </summary>
    [HttpPost("{id}/update-due-date")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
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
            await _auditLogService.LogErrorAsync($"Error updating due date for assignment {id}", "Assignments", ex);
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
            await _auditLogService.LogErrorAsync($"Error deleting assignments for project {projectId}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.ProjectDeleteError"), ex));
        }
    }

    #endregion

    #region İSTATİSTİKLER

    /// <summary>
    /// Get assignment summary statistics
    /// </summary>
    [HttpGet("summary")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetSummary([FromQuery] int? projectId = null)
    {
        try
        {
            var summary = await _assignmentService.GetSummaryAsync(projectId);
            return Ok(summary);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading assignment summary", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.SummaryLoadError"), ex));
        }
    }

    /// <summary>
    /// Get project assignment summaries
    /// </summary>
    [HttpGet("project-summaries")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetProjectSummaries()
    {
        try
        {
            var summaries = await _assignmentService.GetProjectSummariesAsync();
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading project summaries", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.SummaryLoadError"), ex));
        }
    }

    #endregion

    #region SÜRESİ DOLANLAR

    /// <summary>
    /// Get expired assignments
    /// </summary>
    [HttpGet("expired")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetExpired()
    {
        try
        {
            var expired = await _assignmentService.GetExpiredAsync();
            return Ok(expired);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading expired assignments", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.ExpiredLoadError"), ex));
        }
    }

    /// <summary>
    /// Get upcoming due assignments
    /// </summary>
    [HttpGet("upcoming-due")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetUpcomingDue([FromQuery] int daysAhead = 3)
    {
        try
        {
            var upcoming = await _assignmentService.GetUpcomingDueAsync(daysAhead);
            return Ok(upcoming);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading upcoming due assignments", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Assignment.UpcomingLoadError"), ex));
        }
    }

    #endregion

    #region DÖNEMLER (PERIODS)

    /// <summary>
    /// Get periods for an assignment
    /// </summary>
    [HttpGet("{id}/periods")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
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
            await _auditLogService.LogErrorAsync($"Error loading periods for assignment {id}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse("Dönemler yüklenirken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Create a new period for an assignment
    /// </summary>
    [HttpPost("{id}/periods")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
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
            await _auditLogService.LogErrorAsync($"Error creating period for assignment {id}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse("Dönem oluşturulurken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Update a period
    /// </summary>
    [HttpPut("{id}/periods/{periodId}")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
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
            await _auditLogService.LogErrorAsync($"Error updating period {periodId}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse("Dönem güncellenirken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Close a period
    /// </summary>
    [HttpPost("{id}/periods/{periodId}/close")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
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
            await _auditLogService.LogErrorAsync($"Error closing period {periodId}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse("Dönem kapatılırken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Reopen a closed period
    /// </summary>
    [HttpPost("{id}/periods/{periodId}/reopen")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
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
            await _auditLogService.LogErrorAsync($"Error reopening period {periodId}", "Assignments", ex);
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
            await _auditLogService.LogErrorAsync($"Error deleting period {periodId}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse("Dönem silinirken bir hata oluştu.", ex));
        }
    }

    #endregion

    #region ŞUBE (DEALER) YÖNETİMİ

    /// <summary>
    /// Get dealers assigned to an assignment
    /// </summary>
    [HttpGet("{id}/dealers")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector,FieldWorker")]
    public async Task<IActionResult> GetAssignmentDealers(int id)
    {
        try
        {
            var dealers = await _assignmentService.GetAssignmentDealersAsync(id);
            return Ok(dealers);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading dealers for assignment {id}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse("Şubeler yüklenirken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Add a dealer to an assignment
    /// </summary>
    [HttpPost("{id}/dealers")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> AddDealerToAssignment(int id, [FromBody] AddDealerToAssignmentDto dto)
    {
        try
        {
            var dealer = await _assignmentService.AddDealerToAssignmentAsync(id, dto.CustomerDealerId);
            return Ok(dealer);
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
            await _auditLogService.LogErrorAsync($"Error adding dealer to assignment {id}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse("Şube eklenirken bir hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Remove a dealer from an assignment
    /// </summary>
    [HttpDelete("{id}/dealers/{customerDealerId}")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> RemoveDealerFromAssignment(int id, int customerDealerId)
    {
        try
        {
            await _assignmentService.RemoveDealerFromAssignmentAsync(id, customerDealerId);
            return Ok(new { message = "Şube atamadan çıkarıldı." });
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
            await _auditLogService.LogErrorAsync($"Error removing dealer {customerDealerId} from assignment {id}", "Assignments", ex);
            return StatusCode(500, CreateErrorResponse("Şube çıkarılırken bir hata oluştu.", ex));
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

        // Admin ve QualitySpecialist her şeyi görebilir
        if (userRole == "Admin" || userRole == "QualitySpecialist")
        {
            return true;
        }

        // Kullanıcı sadece kendine atanmış assignment'ları görebilir
        if (assignment.AssignedUserId != userId)
        {
            await _auditLogService.LogWarningAsync($"User {userId} attempted to access unauthorized assignment {assignment.Id}", "Assignments");
            return false;
        }

        return true;
    }

    #endregion
}
