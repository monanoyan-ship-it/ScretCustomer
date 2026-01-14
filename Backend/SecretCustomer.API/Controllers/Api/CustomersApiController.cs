using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Customer;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/customers")]
[Authorize(Roles = "Admin")]
public class CustomersApiController : BaseApiController
{
    private readonly ICustomerService _customerService;
    private readonly ILogger<CustomersApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public CustomersApiController(
        ICustomerService customerService,
        ILogger<CustomersApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _customerService = customerService;
        _logger = logger;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Get all customers - optimized with projection (no Include)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
    {
        try
        {
            var customers = await _customerService.GetListAsync(includeInactive);
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customers");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Customer.LoadListError"), ex));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Customer.ActiveLoadError"), ex));
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var customer = await _customerService.GetByIdAsync(id);
            if (customer == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Customer.NotFound")));
            }

            return Ok(customer);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customer {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Customer.LoadError"), ex));
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
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Customer.CreateError"), ex));
        }
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerDto dto)
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
            return NotFound(CreateErrorResponse(ex.Message, ex));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating customer {Id}", id);
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Customer.UpdateError"), ex));
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _customerService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Customer {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting customer {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Customer.DeleteError"), ex));
        }
    }

    /// <summary>
    /// Müşteri personel listesini Excel olarak dışa aktar
    /// </summary>
    [HttpGet("{id}/personnel/export/excel")]
    public async Task<IActionResult> ExportPersonnelToExcel(int id)
    {
        try
        {
            var result = await _customerService.ExportPersonnelToExcelAsync(id);
            if (result == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Customer.NotFound")));
            }

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting personnel for customer {Id}", id);
            return StatusCode(500, CreateErrorResponse("Personel listesi dışa aktarılırken hata oluştu", ex));
        }
    }
}
