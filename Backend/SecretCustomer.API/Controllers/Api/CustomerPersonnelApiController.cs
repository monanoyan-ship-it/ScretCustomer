using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Customer;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/customer-personnel")]
[Authorize(Roles = "Admin")]
public class CustomerPersonnelApiController : BaseApiController
{
    private readonly ICustomerPersonnelService _personnelService;
    private readonly ILogger<CustomerPersonnelApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public CustomerPersonnelApiController(
        ICustomerPersonnelService personnelService,
        ILogger<CustomerPersonnelApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _personnelService = personnelService;
        _logger = logger;
        _localizationService = localizationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        try
        {
            var personnel = await _personnelService.GetAllAsync(includeInactive);
            return Ok(personnel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer personnel");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.CustomerPersonnel.LoadListError"), ex));
        }
    }

    [HttpGet("by-customer/{customerId}")]
    public async Task<IActionResult> GetByCustomerId(int customerId, [FromQuery] bool includeInactive = false)
    {
        try
        {
            var personnel = await _personnelService.GetByCustomerIdAsync(customerId, includeInactive);
            return Ok(personnel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading personnel for customer {CustomerId}", customerId);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.CustomerPersonnel.LoadListError"), ex));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var personnel = await _personnelService.GetByIdAsync(id);
            if (personnel == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.CustomerPersonnel.NotFound")));
            }

            return Ok(personnel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading personnel {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.CustomerPersonnel.LoadError"), ex));
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerPersonnelDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    k => k.Key,
                    v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(CreateErrorResponse("Validasyon hatası", new InvalidOperationException(string.Join("; ", errors.SelectMany(e => e.Value)))));
        }

        try
        {
            var personnel = await _personnelService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = personnel.Id }, personnel);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Customer not found");
            return NotFound(CreateErrorResponse(ex.Message, ex));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating personnel");
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating personnel");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.CustomerPersonnel.CreateError"), ex));
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerPersonnelDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    k => k.Key,
                    v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(CreateErrorResponse("Validasyon hatası", new InvalidOperationException(string.Join("; ", errors.SelectMany(e => e.Value)))));
        }

        try
        {
            var personnel = await _personnelService.UpdateAsync(id, dto);
            return Ok(personnel);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personnel {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message, ex));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating personnel {Id}", id);
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating personnel {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.CustomerPersonnel.UpdateError"), ex));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _personnelService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personnel {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting personnel {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.CustomerPersonnel.DeleteError"), ex));
        }
    }

    [HttpPost("{id}/change-password")]
    [Authorize(Roles = "Admin,CustomerManager,CustomerSupervisor,CustomerOperator")]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] CustomerPersonnelChangePasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    k => k.Key,
                    v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(CreateErrorResponse("Validasyon hatası", new InvalidOperationException(string.Join("; ", errors.SelectMany(e => e.Value)))));
        }

        try
        {
            await _personnelService.ChangePasswordAsync(id, dto);
            return Ok(new { message = await _localizationService.GetResourceAsync("Api.CustomerPersonnel.PasswordChangeSuccess") });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personnel {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message, ex));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid password change attempt for personnel {Id}", id);
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password for personnel {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.CustomerPersonnel.PasswordChangeError"), ex));
        }
    }

    /// <summary>
    /// Admin tarafından şifre sıfırlama - eski şifre gerektirmez
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] AdminResetPasswordDto dto)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    k => k.Key,
                    v => v.Value!.Errors.Select(e => e.ErrorMessage).ToArray()
                );
            return BadRequest(CreateErrorResponse("Validasyon hatası", new InvalidOperationException(string.Join("; ", errors.SelectMany(e => e.Value)))));
        }

        try
        {
            await _personnelService.ResetPasswordAsync(id, dto);
            return Ok(new { message = await _localizationService.GetResourceAsync("Api.CustomerPersonnel.PasswordResetSuccess") });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personnel {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password for personnel {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.CustomerPersonnel.PasswordResetError"), ex));
        }
    }
}
