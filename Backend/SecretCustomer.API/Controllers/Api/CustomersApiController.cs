using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Customer;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Admin")]
public class CustomersApiController : ControllerBase
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public CustomersApiController(
        ICustomerService customerService,
        ILogger<CustomersApiController> logger,
        ILocalizationService localizationService)
    {
        _customerService = customerService;
        _logger = logger;
        _localizationService = localizationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        try
        {
            var customers = await _customerService.GetAllAsync(includeInactive);
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customers");
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Customer.LoadListError") });
        }
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive()
    {
        try
        {
            var customers = await _customerService.GetActiveAsync();
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading active customers");
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Customer.ActiveLoadError") });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound(new { message = await _localizationService.GetResourceAsync("Api.Customer.NotFound") });
            }

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer {Id}", id);
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Customer.LoadError") });
        }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var customer = await _customerService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating customer");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Customer.CreateError") });
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var customer = await _customerService.UpdateAsync(id, dto);
            return Ok(customer);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Customer {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating customer {Id}", id);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {Id}", id);
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Customer.UpdateError") });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _customerService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Customer {Id} not found", id);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {Id}", id);
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.Customer.DeleteError") });
        }
    }
}
