using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Branch;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/branches")]
[Authorize(Roles = "Admin")]
public class BranchesApiController : ControllerBase
{
    private readonly IBranchService _branchService;
    private readonly ILogger<BranchesApiController> _logger;

    public BranchesApiController(IBranchService branchService, ILogger<BranchesApiController> logger)
    {
        _branchService = branchService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        try
        {
            var branches = await _branchService.GetAllAsync(includeInactive);
            return Ok(branches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading branches");
            return StatusCode(500, new { message = "Şubeler yüklenirken bir hata oluştu." });
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var branches = await _branchService.GetActiveAsync();
            return Ok(branches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading active branches");
            return StatusCode(500, new { message = "Aktif şubeler yüklenirken bir hata oluştu." });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var branch = await _branchService.GetByIdAsync(id);
            if (branch == null)
            {
                return NotFound(new { message = "Şube bulunamadı." });
            }

            return Ok(branch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading branch {Id}", id);
            return StatusCode(500, new { message = "Şube yüklenirken bir hata oluştu." });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBranchDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var branch = await _branchService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = branch.Id }, branch);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating branch");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating branch");
            return StatusCode(500, new { message = "Şube oluşturulurken bir hata oluştu." });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBranchDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var branch = await _branchService.UpdateAsync(id, dto);
            return Ok(branch);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Branch {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating branch {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating branch {Id}", id);
            return StatusCode(500, new { message = "Şube güncellenirken bir hata oluştu." });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _branchService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Branch {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting branch {Id}", id);
            return StatusCode(500, new { message = "Şube silinirken bir hata oluştu." });
        }
    }
}
