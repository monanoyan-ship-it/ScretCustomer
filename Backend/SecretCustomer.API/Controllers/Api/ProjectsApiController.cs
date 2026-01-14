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
    private readonly ILogger<ProjectsApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public ProjectsApiController(
        IProjectService projectService,
        ILogger<ProjectsApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _projectService = projectService;
        _logger = logger;
        _localizationService = localizationService;
    }

    #region CRUD Operations

    /// <summary>
    /// Tüm projeleri getir - Optimize edilmiş liste
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search = null,
        [FromQuery] int? customerId = null,
        [FromQuery] string? projectType = null,
        [FromQuery] string? status = null,
        [FromQuery] int? projectManagerId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] bool includeInactive = false)
    {
        try
        {
            // Optimize edilmiş liste methodu kullan
            var projects = await _projectService.GetListAsync(
                search, customerId, projectType, status, projectManagerId, startDate, endDate, includeInactive);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading projects");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.LoadListError"), ex));
        }
    }

    /// <summary>
    /// Proje özetlerini getir (Dashboard için)
    /// </summary>
    [HttpGet("summaries")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetSummaries()
    {
        try
        {
            var summaries = await _projectService.GetSummariesAsync();
            return Ok(summaries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading project summaries");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.SummaryLoadError"), ex));
        }
    }

    /// <summary>
    /// Proje ID ile getir
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,TeamLeader")]
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
            _logger.LogError(ex, "Error loading project {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.LoadError"), ex));
        }
    }

    /// <summary>
    /// Proje detaylarını getir (Şubeler ve Takım üyeleri dahil)
    /// </summary>
    [HttpGet("{id}/detail")]
    [Authorize(Roles = "Admin,TeamLeader")]
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
            _logger.LogError(ex, "Error loading project detail {Id}", id);
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
            _logger.LogError(ex, "Error creating project");
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
            _logger.LogError(ex, "Error updating project {Id}", id);
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
            _logger.LogError(ex, "Error deleting project {Id}", id);
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
            _logger.LogError(ex, "Error updating project status {Id}", id);
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
            _logger.LogError(ex, "Error starting project {Id}", id);
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
            _logger.LogError(ex, "Error pausing project {Id}", id);
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
            _logger.LogError(ex, "Error completing project {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.CompleteError"), ex));
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
            _logger.LogError(ex, "Error canceling project {Id}", id);
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
            _logger.LogError(ex, "Error closing project {Id}", id);
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
            _logger.LogError(ex, "Error managing project team {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.TeamUpdateError"), ex));
        }
    }

    #endregion

    #region Branch Management

    /// <summary>
    /// Proje şubelerini yönet (ekle/çıkar)
    /// </summary>
    [HttpPost("{id}/branches")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ManageBranches(int id, [FromBody] ManageProjectBranchesDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var project = await _projectService.ManageBranchesAsync(id, dto);
            return Ok(project);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.NotFound")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error managing project branches {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.BranchesUpdateError"), ex));
        }
    }

    #endregion

    #region Statistics & Queries

    /// <summary>
    /// Proje istatistiklerini getir
    /// </summary>
    [HttpGet("{id}/statistics")]
    [Authorize(Roles = "Admin,TeamLeader")]
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
            _logger.LogError(ex, "Error loading project statistics {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.StatisticsLoadError"), ex));
        }
    }

    /// <summary>
    /// Müşteri bazlı projeleri getir
    /// </summary>
    [HttpGet("by-customer/{customerId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        try
        {
            var projects = await _projectService.GetByCustomerIdAsync(customerId);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading projects by customer {CustomerId}", customerId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.CustomerProjectsLoadError"), ex));
        }
    }

    /// <summary>
    /// Proje yöneticisi bazlı projeleri getir
    /// </summary>
    [HttpGet("by-manager/{managerId}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetByManager(int managerId)
    {
        try
        {
            var projects = await _projectService.GetByManagerIdAsync(managerId);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading projects by manager {ManagerId}", managerId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.ManagerProjectsLoadError"), ex));
        }
    }

    /// <summary>
    /// Aktif projeleri getir
    /// </summary>
    [HttpGet("active")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetActiveProjects()
    {
        try
        {
            var projects = await _projectService.GetActiveProjectsAsync();
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading active projects");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Project.ActiveProjectsLoadError"), ex));
        }
    }

    /// <summary>
    /// Yaklaşan bitiş tarihli projeleri getir
    /// </summary>
    [HttpGet("upcoming-deadlines")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> GetUpcomingDeadlines([FromQuery] int daysAhead = 7)
    {
        try
        {
            var projects = await _projectService.GetUpcomingDeadlinesAsync(daysAhead);
            return Ok(projects);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading upcoming deadline projects");
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
            _logger.LogError(ex, "Error generating project code");
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
