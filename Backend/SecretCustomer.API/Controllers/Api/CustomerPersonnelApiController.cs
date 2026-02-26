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
    private readonly IAuditLogService _auditLogService;
    private readonly ILocalizationService _localizationService;

    public CustomerPersonnelApiController(
        ICustomerPersonnelService personnelService,
        IAuditLogService auditLogService,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _personnelService = personnelService;
        _auditLogService = auditLogService;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Get customer personnel with server-side filtering, pagination
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PersonnelFilterDto filter)
    {
        try
        {
            var result = await _personnelService.GetFilteredListAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading customer personnel", "CustomerPersonnel", ex);
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
            await _auditLogService.LogErrorAsync($"Error loading personnel for customer {customerId}", "CustomerPersonnel", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.CustomerPersonnel.LoadListError"), ex));
        }
    }

    [HttpGet("by-organization/{organizationId}")]
    public async Task<IActionResult> GetByOrganizationId(int organizationId, [FromQuery] bool includeInactive = false)
    {
        try
        {
            var personnel = await _personnelService.GetByOrganizationIdAsync(organizationId, includeInactive);
            return Ok(personnel);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading personnel for organization {organizationId}", "CustomerPersonnel", ex);
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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.NotFound")));
            }

            return Ok(personnel);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading personnel {id}", "CustomerPersonnel", ex);
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
            await _auditLogService.LogWarningAsync($"Customer not found", "CustomerPersonnel");
            return NotFound(CreateErrorResponse(ex.Message, ex));
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogWarningAsync($"Validation error while creating personnel", "CustomerPersonnel");
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error creating personnel", "CustomerPersonnel", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.CreateError"), ex));
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
            await _auditLogService.LogWarningAsync($"Personnel {id} not found", "CustomerPersonnel");
            return NotFound(CreateErrorResponse(ex.Message, ex));
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogWarningAsync($"Validation error while updating personnel {id}", "CustomerPersonnel");
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating personnel {id}", "CustomerPersonnel", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.UpdateError"), ex));
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
            await _auditLogService.LogWarningAsync($"Personnel {id} not found", "CustomerPersonnel");
            return NotFound(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error deleting personnel {id}", "CustomerPersonnel", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Personnel.DeleteError"), ex));
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
            return Ok(new { message = await _localizationService.GetResourceAsync("Api.User.PasswordChangeSuccess") });
        }
        catch (KeyNotFoundException ex)
        {
            await _auditLogService.LogWarningAsync($"Personnel {id} not found", "CustomerPersonnel");
            return NotFound(CreateErrorResponse(ex.Message, ex));
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogWarningAsync($"Invalid password change attempt for personnel {id}", "CustomerPersonnel");
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error changing password for personnel {id}", "CustomerPersonnel", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.CustomerPersonnel.PasswordChangeError"), ex));
        }
    }

    [HttpGet("check-username/{username}")]
    public async Task<IActionResult> CheckUsername(string username, [FromQuery] int? excludeId = null)
    {
        try
        {
            var exists = await _personnelService.ExistsByUsernameAsync(username, excludeId);
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error checking username {username}", "CustomerPersonnel", ex);
            return StatusCode(500, CreateErrorResponse("Kullanıcı adı kontrolü sırasında hata oluştu.", ex));
        }
    }

    [HttpGet("check-email/{email}")]
    public async Task<IActionResult> CheckEmail(string email, [FromQuery] int? excludeId = null)
    {
        try
        {
            var exists = await _personnelService.ExistsByEmailAsync(email, excludeId);
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error checking email {email}", "CustomerPersonnel", ex);
            return StatusCode(500, CreateErrorResponse("E-posta kontrolü sırasında hata oluştu.", ex));
        }
    }

    [HttpPut("{id}/change-organization")]
    public async Task<IActionResult> ChangeOrganization(int id, [FromBody] ChangeOrganizationDto dto)
    {
        try
        {
            await _personnelService.ChangeOrganizationAsync(id, dto.NewOrganizationId);
            return Ok(new { message = "Personel organizasyonu güncellendi." });
        }
        catch (KeyNotFoundException ex)
        {
            await _auditLogService.LogWarningAsync($"Personnel {id} not found", "CustomerPersonnel");
            return NotFound(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error changing organization for personnel {id}", "CustomerPersonnel", ex);
            return StatusCode(500, CreateErrorResponse("Organizasyon değiştirme sırasında hata oluştu.", ex));
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
            await _auditLogService.LogWarningAsync($"Personnel {id} not found", "CustomerPersonnel");
            return NotFound(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error resetting password for personnel {id}", "CustomerPersonnel", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Account.PasswordResetError"), ex));
        }
    }
}
