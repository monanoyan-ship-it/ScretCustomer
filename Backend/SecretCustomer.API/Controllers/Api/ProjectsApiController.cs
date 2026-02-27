using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Project;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/projects")]
[Authorize]
public class ProjectsApiController : BaseApiController
{
    private readonly IProjectService _projectService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILocalizationService _localizationService;

    public ProjectsApiController(
        IProjectService projectService,
        IAuditLogService auditLogService,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _projectService = projectService;
        _auditLogService = auditLogService;
        _localizationService = localizationService;
    }

    #region CRUD Operations

    /// <summary>
    /// Tüm projeleri getir - Optimize edilmiş liste
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetAll([FromQuery] ProjectFilterDto filter)
    {
        try
        {
            var projects = await _projectService.GetListAsync(filter);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error loading projects", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.LoadListError"), ex));
        }
    }

    /// <summary>
    /// Proje özetlerini getir (Dashboard için)
    /// </summary>
    [HttpGet("summaries")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetSummaries()
    {
        try
        {
            var summaries = await _projectService.GetSummariesAsync();
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error loading project summaries", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.SummaryLoadError"), ex));
        }
    }

    /// <summary>
    /// Proje ID ile getir
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var project = await _projectService.GetByIdAsync(id);
            if (project == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
            }
            return Ok(project);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading project {id}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.LoadError"), ex));
        }
    }

    /// <summary>
    /// Proje detaylarını getir (Şubeler ve Takım üyeleri dahil)
    /// </summary>
    [HttpGet("{id}/detail")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetDetail(int id)
    {
        try
        {
            var project = await _projectService.GetDetailByIdAsync(id);
            if (project == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
            }
            return Ok(project);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading project detail {id}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.DetailLoadError"), ex));
        }
    }

    /// <summary>
    /// Yeni proje oluştur
    /// </summary>
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
            await _auditLogService.LogErrorAsync("Error creating project", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.CreateError"), ex));
        }
    }

    /// <summary>
    /// Proje güncelle
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateProjectDto dto)
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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
            }
            return Ok(project);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating project {id}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.UpdateError"), ex));
        }
    }

    /// <summary>
    /// Proje sil
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _projectService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
            }
            return NoContent();
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error deleting project {id}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.DeleteError"), ex));
        }
    }

    #endregion

    #region Status Management

    /// <summary>
    /// Proje durumunu güncelle
    /// </summary>
    [HttpPost("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateProjectStatusDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var project = await _projectService.UpdateStatusAsync(id, dto);
            return Ok(project);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating project status {id}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.StatusUpdateError"), ex));
        }
    }

    /// <summary>
    /// Projeyi başlat
    /// </summary>
    [HttpPost("{id}/start")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> StartProject(int id)
    {
        try
        {
            var project = await _projectService.StartProjectAsync(id);
            return Ok(project);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error starting project {id}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.StartError"), ex));
        }
    }

    /// <summary>
    /// Projeyi duraklat
    /// </summary>
    [HttpPost("{id}/pause")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> PauseProject(int id)
    {
        try
        {
            var project = await _projectService.PauseProjectAsync(id);
            return Ok(project);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error pausing project {id}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.PauseError"), ex));
        }
    }

    /// <summary>
    /// Projeyi tamamla
    /// </summary>
    [HttpPost("{id}/complete")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CompleteProject(int id)
    {
        try
        {
            var project = await _projectService.CompleteProjectAsync(id);
            return Ok(project);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error completing project {id}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.CompleteError"), ex));
        }
    }

    /// <summary>
    /// Tamamlanan projeyi yeniden aktif et
    /// </summary>
    [HttpPost("{id}/reactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ReactivateProject(int id)
    {
        try
        {
            var project = await _projectService.ReactivateProjectAsync(id);
            return Ok(project);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error reactivating project {id}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse("Proje yeniden aktif edilirken bir hata oluştu."));
        }
    }

    /// <summary>
    /// Projeyi iptal et
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CancelProject(int id, [FromBody] CancelProjectDto? dto = null)
    {
        try
        {
            var project = await _projectService.CancelProjectAsync(id, dto?.Reason);
            return Ok(project);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error canceling project {id}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.CancelError"), ex));
        }
    }

    /// <summary>
    /// Projeyi kapat (eski endpoint - geriye uyumluluk için)
    /// </summary>
    [HttpPost("{id}/close")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CloseProject(int id)
    {
        try
        {
            var project = await _projectService.CloseProjectAsync(id);
            return Ok(project);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error closing project {id}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.CloseError"), ex));
        }
    }

    #endregion

    #region Team Management

    /// <summary>
    /// Proje takımını yönet (ekle/çıkar)
    /// </summary>
    [HttpPost("{id}/team")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ManageTeam(int id, [FromBody] ManageProjectTeamDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var project = await _projectService.ManageTeamAsync(id, dto);
            return Ok(project);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error managing project team {id}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.TeamUpdateError"), ex));
        }
    }

    #endregion

    #region Statistics & Queries

    /// <summary>
    /// Proje istatistiklerini getir
    /// </summary>
    [HttpGet("{id}/statistics")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetStatistics(int id, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        try
        {
            var project = await _projectService.GetStatisticsAsync(id, startDate, endDate);
            if (project == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
            }
            return Ok(project);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading project statistics {id}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.StatisticsLoadError"), ex));
        }
    }

    /// <summary>
    /// Müşteri bazlı projeleri getir
    /// </summary>
    [HttpGet("by-customer/{customerId}")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        try
        {
            var projects = await _projectService.GetByCustomerIdAsync(customerId);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading projects by customer {customerId}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.CustomerProjectsLoadError"), ex));
        }
    }

    /// <summary>
    /// Proje yöneticisi bazlı projeleri getir
    /// </summary>
    [HttpGet("by-manager/{managerId}")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetByManager(int managerId)
    {
        try
        {
            var projects = await _projectService.GetByManagerIdAsync(managerId);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading projects by manager {managerId}", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.ManagerProjectsLoadError"), ex));
        }
    }

    /// <summary>
    /// Aktif projeleri getir
    /// </summary>
    [HttpGet("active")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetActiveProjects()
    {
        try
        {
            var projects = await _projectService.GetActiveProjectsAsync();
            return Ok(projects);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error loading active projects", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.ActiveProjectsLoadError"), ex));
        }
    }

    /// <summary>
    /// Yaklaşan bitiş tarihli projeleri getir
    /// </summary>
    [HttpGet("upcoming-deadlines")]
    [Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
    public async Task<IActionResult> GetUpcomingDeadlines([FromQuery] int daysAhead = 7)
    {
        try
        {
            var projects = await _projectService.GetUpcomingDeadlinesAsync(daysAhead);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error loading upcoming deadline projects", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.UpcomingDeadlinesLoadError"), ex));
        }
    }

    /// <summary>
    /// Yeni proje kodu oluştur
    /// </summary>
    [HttpGet("generate-code")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerateCode()
    {
        try
        {
            var code = await _projectService.GenerateProjectCodeAsync();
            return Ok(new { code });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error generating project code", "Projects", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.CodeGenerationError"), ex));
        }
    }

    #endregion
}

/// <summary>
/// Proje iptal için DTO
/// </summary>
public class CancelProjectDto
{
    public string? Reason { get; set; }
}
