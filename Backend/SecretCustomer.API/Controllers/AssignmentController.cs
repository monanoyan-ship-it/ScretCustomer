using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Assignment;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AssignmentController : ControllerBase
{
    private readonly IAssignmentService _assignmentService;
    private readonly ILogger<AssignmentController> _logger;

    public AssignmentController(
        IAssignmentService assignmentService,
        ILogger<AssignmentController> logger)
    {
        _assignmentService = assignmentService;
        _logger = logger;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AssignmentDto>> GetById(Guid id)
    {
        try
        {
            var assignment = await _assignmentService.GetByIdAsync(id);
            if (assignment == null)
                return NotFound($"Assignment with ID {id} not found");

            return Ok(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignment {AssignmentId}", id);
            return StatusCode(500, "An error occurred while retrieving the assignment");
        }
    }

    [HttpGet("link/{uniqueLink}")]
    public async Task<ActionResult<AssignmentDto>> GetByUniqueLink(string uniqueLink)
    {
        try
        {
            var assignment = await _assignmentService.GetByUniqueLinkAsync(uniqueLink);
            if (assignment == null)
                return NotFound($"Assignment with link {uniqueLink} not found");

            return Ok(assignment);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignment by link {UniqueLink}", uniqueLink);
            return StatusCode(500, "An error occurred while retrieving the assignment");
        }
    }

    [HttpGet("project/{projectId}")]
    public async Task<ActionResult<IEnumerable<AssignmentDto>>> GetByProjectId(Guid projectId)
    {
        try
        {
            var assignments = await _assignmentService.GetByProjectIdAsync(projectId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignments for project {ProjectId}", projectId);
            return StatusCode(500, "An error occurred while retrieving assignments");
        }
    }

    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<AssignmentDto>>> GetByUserId(Guid userId)
    {
        try
        {
            var assignments = await _assignmentService.GetByUserIdAsync(userId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignments for user {UserId}", userId);
            return StatusCode(500, "An error occurred while retrieving assignments");
        }
    }

    [HttpGet("branch/{branchId}")]
    public async Task<ActionResult<IEnumerable<AssignmentDto>>> GetByBranchId(Guid branchId)
    {
        try
        {
            var assignments = await _assignmentService.GetByBranchIdAsync(branchId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting assignments for branch {BranchId}", branchId);
            return StatusCode(500, "An error occurred while retrieving assignments");
        }
    }

    [HttpPost]
    public async Task<ActionResult<AssignmentDto>> Create([FromBody] CreateAssignmentDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _assignmentService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Related entity not found");
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignment");
            return StatusCode(500, "An error occurred while creating the assignment");
        }
    }

    [HttpPost("bulk")]
    public async Task<ActionResult<IEnumerable<AssignmentDto>>> CreateBulk([FromBody] BulkAssignmentDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _assignmentService.CreateBulkAsync(dto);
            return Ok(created);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Related entity not found");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bulk assignments");
            return StatusCode(500, "An error occurred while creating assignments");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _assignmentService.DeleteAsync(id);
            if (!result)
                return NotFound($"Assignment with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assignment {AssignmentId}", id);
            return StatusCode(500, "An error occurred while deleting the assignment");
        }
    }

    [HttpPost("{id}/complete")]
    public async Task<ActionResult<AssignmentDto>> CompleteAssignment(Guid id)
    {
        try
        {
            var completed = await _assignmentService.CompleteAssignmentAsync(id);
            return Ok(completed);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Assignment {AssignmentId} not found", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error completing assignment {AssignmentId}", id);
            return StatusCode(500, "An error occurred while completing the assignment");
        }
    }
}
