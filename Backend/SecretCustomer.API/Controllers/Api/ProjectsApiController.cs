using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Project;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsApiController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly ILogger<ProjectsApiController> _logger;

    public ProjectsApiController(IProjectService projectService, ILogger<ProjectsApiController> logger)
    {
        _projectService = projectService;
        _logger = logger;
    }

    /// <summary>
    /// Get all projects - Only Admin and TeamLeader can access
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var projects = await _projectService.GetAllAsync();
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading projects");
            return StatusCode(500, new { message = "Projeler yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Get project by ID - Only Admin and TeamLeader can access
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            _logger.LogInformation("Loading project {Id}", id);
            var project = await _projectService.GetByIdAsync(id);

            if (project == null)
            {
                _logger.LogWarning("Project {Id} not found", id);
                return NotFound(new { message = "Proje bulunamadı." });
            }

            _logger.LogInformation("Successfully loaded project {Id}", id);
            return Ok(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading project {Id}. Exception: {Message}", id, ex.Message);

            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";

            return StatusCode(500, new {
                message = "Proje yüklenirken bir hata oluştu.",
                error = isDevelopment ? ex.Message : null,
                details = isDevelopment ? ex.StackTrace : null
            });
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProjectDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var project = await _projectService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating project");
            return StatusCode(500, new { message = "Proje oluşturulurken bir hata oluştu." });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] CreateProjectDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var project = await _projectService.UpdateAsync(id, dto);
            if (project == null)
            {
                return NotFound(new { message = "Proje bulunamadı." });
            }

            return Ok(project);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating project {Id}", id);
            return StatusCode(500, new { message = "Proje güncellenirken bir hata oluştu." });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _projectService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(new { message = "Proje bulunamadı." });
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project {Id}", id);
            return StatusCode(500, new { message = "Proje silinirken bir hata oluştu." });
        }
    }
}
