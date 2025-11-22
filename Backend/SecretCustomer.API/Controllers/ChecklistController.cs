using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Checklist;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChecklistController : ControllerBase
{
    private readonly IChecklistService _checklistService;
    private readonly ILogger<ChecklistController> _logger;

    public ChecklistController(
        IChecklistService checklistService,
        ILogger<ChecklistController> logger)
    {
        _checklistService = checklistService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm kontrol listelerini getirir
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ChecklistDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        try
        {
            var checklists = await _checklistService.GetAllAsync(includeInactive);
            return Ok(checklists);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all checklists");
            return StatusCode(500, "An error occurred while retrieving checklists");
        }
    }

    /// <summary>
    /// ID'ye göre kontrol listesi getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ChecklistDto>> GetById(Guid id)
    {
        try
        {
            var checklist = await _checklistService.GetByIdAsync(id);
            if (checklist == null)
                return NotFound($"Checklist with ID {id} not found");

            return Ok(checklist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting checklist {ChecklistId}", id);
            return StatusCode(500, "An error occurred while retrieving the checklist");
        }
    }

    /// <summary>
    /// Yeni kontrol listesi oluşturur
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ChecklistDto>> Create([FromBody] CreateChecklistDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var created = await _checklistService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating checklist");
            return StatusCode(500, "An error occurred while creating the checklist");
        }
    }

    /// <summary>
    /// Kontrol listesini günceller
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<ChecklistDto>> Update(Guid id, [FromBody] UpdateChecklistDto dto)
    {
        try
        {
            if (id != dto.Id)
                return BadRequest("ID mismatch");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var updated = await _checklistService.UpdateAsync(dto);
            return Ok(updated);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Checklist {ChecklistId} not found for update", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating checklist {ChecklistId}", id);
            return StatusCode(500, "An error occurred while updating the checklist");
        }
    }

    /// <summary>
    /// Kontrol listesini siler (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _checklistService.DeleteAsync(id);
            if (!result)
                return NotFound($"Checklist with ID {id} not found");

            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting checklist {ChecklistId}", id);
            return StatusCode(500, "An error occurred while deleting the checklist");
        }
    }

    /// <summary>
    /// Kontrol listesini klonlar (yeni versiyon oluşturur)
    /// </summary>
    [HttpPost("{id}/clone")]
    public async Task<ActionResult<ChecklistDto>> Clone(Guid id, [FromBody] CloneChecklistRequest request)
    {
        try
        {
            var cloned = await _checklistService.CloneChecklistAsync(id, request.NewName);
            return CreatedAtAction(nameof(GetById), new { id = cloned.Id }, cloned);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Checklist {ChecklistId} not found for cloning", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning checklist {ChecklistId}", id);
            return StatusCode(500, "An error occurred while cloning the checklist");
        }
    }
}

public record CloneChecklistRequest(string NewName);
