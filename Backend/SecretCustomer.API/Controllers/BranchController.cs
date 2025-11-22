using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Branch;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BranchController : ControllerBase
{
    private readonly IBranchService _branchService;
    private readonly ILogger<BranchController> _logger;

    public BranchController(
        IBranchService branchService,
        ILogger<BranchController> logger)
    {
        _branchService = branchService;
        _logger = logger;
    }

    /// <summary>
    /// Tüm şubeleri getirir
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<ActionResult<IEnumerable<BranchDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        try
        {
            var branches = await _branchService.GetAllAsync(includeInactive);
            return Ok(branches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all branches");
            return StatusCode(500, "An error occurred while retrieving branches");
        }
    }

    /// <summary>
    /// Aktif şubeleri getirir
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<IEnumerable<BranchDto>>> GetActive()
    {
        try
        {
            var branches = await _branchService.GetActiveAsync();
            return Ok(branches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active branches");
            return StatusCode(500, "An error occurred while retrieving active branches");
        }
    }

    /// <summary>
    /// ID'ye göre şube getirir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<BranchDto>> GetById(Guid id)
    {
        try
        {
            var branch = await _branchService.GetByIdAsync(id);
            if (branch == null)
                return NotFound($"Branch with ID {id} not found");

            return Ok(branch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branch {BranchId}", id);
            return StatusCode(500, "An error occurred while retrieving the branch");
        }
    }

    /// <summary>
    /// Kod'a göre şube getirir
    /// </summary>
    [HttpGet("code/{code}")]
    public async Task<ActionResult<BranchDto>> GetByCode(string code)
    {
        try
        {
            var branch = await _branchService.GetByCodeAsync(code);
            if (branch == null)
                return NotFound($"Branch with code {code} not found");

            return Ok(branch);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branch by code {Code}", code);
            return StatusCode(500, "An error occurred while retrieving the branch");
        }
    }

    /// <summary>
    /// Bölgeye göre şubeleri getirir
    /// </summary>
    [HttpGet("region/{region}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<ActionResult<IEnumerable<BranchDto>>> GetByRegion(string region)
    {
        try
        {
            var branches = await _branchService.GetByRegionAsync(region);
            return Ok(branches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branches by region {Region}", region);
            return StatusCode(500, "An error occurred while retrieving branches");
        }
    }

    /// <summary>
    /// Şehre göre şubeleri getirir
    /// </summary>
    [HttpGet("city/{city}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<ActionResult<IEnumerable<BranchDto>>> GetByCity(string city)
    {
        try
        {
            var branches = await _branchService.GetByCityAsync(city);
            return Ok(branches);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branches by city {City}", city);
            return StatusCode(500, "An error occurred while retrieving branches");
        }
    }

    /// <summary>
    /// Yeni şube oluşturur (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BranchDto>> Create([FromBody] CreateBranchDto createBranchDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var branch = await _branchService.CreateAsync(createBranchDto);
            return CreatedAtAction(nameof(GetById), new { id = branch.Id }, branch);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while creating branch");
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating branch");
            return StatusCode(500, "An error occurred while creating the branch");
        }
    }

    /// <summary>
    /// Şube bilgilerini günceller (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<BranchDto>> Update(Guid id, [FromBody] UpdateBranchDto updateBranchDto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var branch = await _branchService.UpdateAsync(id, updateBranchDto);
            return Ok(branch);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Branch {BranchId} not found for update", id);
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating branch {BranchId}", id);
            return BadRequest(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating branch {BranchId}", id);
            return StatusCode(500, "An error occurred while updating the branch");
        }
    }

    /// <summary>
    /// Şubeyi siler (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> Delete(Guid id)
    {
        try
        {
            await _branchService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Branch {BranchId} not found for deletion", id);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting branch {BranchId}", id);
            return StatusCode(500, "An error occurred while deleting the branch");
        }
    }
}
