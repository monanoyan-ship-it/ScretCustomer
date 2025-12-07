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

    [HttpPost]
    [Authorize(Roles = "Admin")]
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

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
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

    [HttpGet("my-assignments")]
    [Authorize(Roles = "FieldWorker")]
    public async Task<IActionResult> GetMyAssignments()
    {
        try
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = "Kullanıcı bilgisi bulunamadı." });
            }

            var assignments = await _assignmentService.GetByUserIdAsync(userId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user assignments");
            return StatusCode(500, new { message = "Atamalar yüklenirken bir hata oluştu." });
        }
    }
}
