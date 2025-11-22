using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Project;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProjectController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectController> _logger;

    public ProjectController(
        IProjectService projectService,
        ILogger<ProjectController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProjectDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        try
        {
            var projects = await _projectService.GetAllAsync(includeInactive);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all projects");
            return StatusCode(500, "An error occurred while retrieving projects");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProjectDto>> GetById(Guid id)
    {
        try
        {
            var project = await _projectService.GetByIdAsync(id);
            if (project == null)
                return NotFound($"Project with ID {id} not found");

            return Ok(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting project {ProjectId}", id);
            return StatusCode(500, "An error occurred while retrieving the project");
        }
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDto>> Create([FromBody] CreateProjectDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _projectService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Checklist not found");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            return StatusCode(500, "An error occurred while creating the project");
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ProjectDto>> Update(Guid id, [FromBody] CreateProjectDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _projectService.UpdateAsync(id, dto);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Project or checklist not found");
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {ProjectId}", id);
            return StatusCode(500, "An error occurred while updating the project");
        }
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _projectService.DeleteAsync(id);
            if (!result)
                return NotFound($"Project with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project {ProjectId}", id);
            return StatusCode(500, "An error occurred while deleting the project");
        }
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult<ProjectDto>> CloseProject(Guid id)
    {
        try
        {
            var closed = await _projectService.CloseProjectAsync(id);
            return Ok(closed);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Project {ProjectId} not found", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing project {ProjectId}", id);
            return StatusCode(500, "An error occurred while closing the project");
        }
    }
}
