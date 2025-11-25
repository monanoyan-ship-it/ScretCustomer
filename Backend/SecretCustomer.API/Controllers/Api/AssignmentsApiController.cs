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
    private readonly ILogger<AssignmentsApiController> _logger;

    public AssignmentsApiController(IAssignmentService assignmentService, ILogger<AssignmentsApiController> logger)
    {
        _assignmentService = assignmentService;
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
}
