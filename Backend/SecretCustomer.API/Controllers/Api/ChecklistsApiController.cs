using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Checklist;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/checklists")]
[Authorize]
public class ChecklistsApiController : BaseApiController
{
    private readonly IChecklistService _checklistService;
    private readonly ILogger<ChecklistsApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public ChecklistsApiController(
        IChecklistService checklistService,
        ILogger<ChecklistsApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _checklistService = checklistService;
        _logger = logger;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Get all checklists - Admin, TeamLeader, and Evaluator can access
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,TeamLeader,Evaluator")]
    public async Task<IActionResult> GetAll()
    {
        try
        {
            var checklists = await _checklistService.GetAllAsync();
            return Ok(checklists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading checklists");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.LoadListError"), ex));
        }
    }

    /// <summary>
    /// Get checklist by ID - Admin, TeamLeader, and Evaluator can access
    /// </summary>
    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,TeamLeader,Evaluator")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            _logger.LogInformation("Loading checklist {Id}", id);
            var checklist = await _checklistService.GetByIdAsync(id);

            if (checklist == null)
            {
                _logger.LogWarning("Checklist {Id} not found", id);
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.NotFound")));
            }

            _logger.LogInformation("Successfully loaded checklist {Id} with {SectionCount} sections",
                id, checklist.Sections?.Count ?? 0);

            return Ok(checklist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading checklist {Id}. Exception: {Message}, StackTrace: {StackTrace}",
                id, ex.Message, ex.StackTrace);

            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.LoadError"), ex));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateChecklistDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var checklist = await _checklistService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = checklist.Id }, checklist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checklist");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.CreateError"), ex));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateChecklistDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            dto.Id = id;
            var checklist = await _checklistService.UpdateAsync(dto);
            if (checklist == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.NotFound")));
            }

            return Ok(checklist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating checklist {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.UpdateError"), ex));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _checklistService.DeleteAsync(id);
            if (!result)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.NotFound")));
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting checklist {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.DeleteError"), ex));
        }
    }

    [HttpPost("{id}/clone")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Clone(Guid id, [FromBody] string newName)
    {
        try
        {
            var checklist = await _checklistService.CloneChecklistAsync(id, newName);
            if (checklist == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.NotFound")));
            }

            return CreatedAtAction(nameof(GetById), new { id = checklist.Id }, checklist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning checklist {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Checklist.CloneError"), ex));
        }
    }
}
