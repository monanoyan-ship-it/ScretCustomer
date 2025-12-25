using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.FieldWorker;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/fieldworkers")]
[Authorize(Roles = "Admin")]
public class FieldWorkersApiController : BaseApiController
{
    private readonly IFieldWorkerService _fieldWorkerService;
    private readonly ILogger<FieldWorkersApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public FieldWorkersApiController(
        IFieldWorkerService fieldWorkerService,
        ILogger<FieldWorkersApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _fieldWorkerService = fieldWorkerService;
        _logger = logger;
        _localizationService = localizationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        try
        {
            var fieldWorkers = await _fieldWorkerService.GetAllAsync(includeInactive);
            return Ok(fieldWorkers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading field workers");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.FieldWorker.LoadListError"), ex));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var fieldWorker = await _fieldWorkerService.GetByIdAsync(id);
            if (fieldWorker == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.FieldWorker.NotFound")));
            }

            return Ok(fieldWorker);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading field worker {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.FieldWorker.LoadError"), ex));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFieldWorkerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var fieldWorker = await _fieldWorkerService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = fieldWorker.Id }, fieldWorker);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating field worker");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating field worker");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.FieldWorker.CreateError"), ex));
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateFieldWorkerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            dto.Id = id;
            var fieldWorker = await _fieldWorkerService.UpdateAsync(id, dto);
            return Ok(fieldWorker);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Field worker {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating field worker {Id}", id);
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating field worker {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.FieldWorker.UpdateError"), ex));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _fieldWorkerService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Field worker {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting field worker {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.FieldWorker.DeleteError"), ex));
        }
    }
}
