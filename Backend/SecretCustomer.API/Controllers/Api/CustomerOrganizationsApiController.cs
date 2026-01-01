using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.CustomerOrganization;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/customer-organizations")]
[Authorize(Roles = "Admin")]
public class CustomerOrganizationsApiController : BaseApiController
{
    private readonly ICustomerOrganizationService _organizationService;
    private readonly ILogger<CustomerOrganizationsApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public CustomerOrganizationsApiController(
        ICustomerOrganizationService organizationService,
        ILogger<CustomerOrganizationsApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _organizationService = organizationService;
        _logger = logger;
        _localizationService = localizationService;
    }

    /// <summary>
    /// DEBUG: Organizasyon ve personel ilişkisini kontrol et
    /// </summary>
    [HttpGet("debug/check-personnel")]
    public async Task<IActionResult> DebugCheckPersonnel()
    {
        try
        {
            var result = await _organizationService.DebugCheckPersonnelAsync();
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Müşteriye ait organizasyonları getir
    /// </summary>
    [HttpGet("by-customer/{customerId}")]
    public async Task<IActionResult> GetByCustomerId(int customerId, [FromQuery] bool includeInactive = false)
    {
        try
        {
            var organizations = await _organizationService.GetByCustomerIdAsync(customerId, includeInactive);
            return Ok(organizations);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading organizations for customer {CustomerId}", customerId);
            return StatusCode(500, CreateErrorResponse("Organizasyonlar yüklenirken bir hata oluştu", ex));
        }
    }

    /// <summary>
    /// Müşteriye ait organizasyon ağacını getir (hiyerarşik)
    /// </summary>
    [HttpGet("tree/{customerId}")]
    public async Task<IActionResult> GetOrganizationTree(int customerId)
    {
        try
        {
            var tree = await _organizationService.GetOrganizationTreeAsync(customerId);
            return Ok(tree);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading organization tree for customer {CustomerId}", customerId);
            return StatusCode(500, CreateErrorResponse("Organizasyon ağacı yüklenirken bir hata oluştu", ex));
        }
    }

    /// <summary>
    /// Organizasyon detayını getir
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var organization = await _organizationService.GetByIdAsync(id);
            if (organization == null)
            {
                return NotFound(CreateErrorResponse("Organizasyon bulunamadı"));
            }
            return Ok(organization);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading organization {Id}", id);
            return StatusCode(500, CreateErrorResponse("Organizasyon yüklenirken bir hata oluştu", ex));
        }
    }

    /// <summary>
    /// Yeni organizasyon oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerOrganizationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var organization = await _organizationService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = organization.Id }, organization);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Customer or parent organization not found");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while creating organization");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating organization");
            return StatusCode(500, CreateErrorResponse("Organizasyon oluşturulurken bir hata oluştu", ex));
        }
    }

    /// <summary>
    /// Organizasyonu güncelle
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCustomerOrganizationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var organization = await _organizationService.UpdateAsync(id, dto);
            return Ok(organization);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Organization {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while updating organization {Id}", id);
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating organization {Id}", id);
            return StatusCode(500, CreateErrorResponse("Organizasyon güncellenirken bir hata oluştu", ex));
        }
    }

    /// <summary>
    /// Organizasyonun parent'ını değiştir (Drag & Drop için)
    /// </summary>
    [HttpPut("{id}/move")]
    public async Task<IActionResult> MoveOrganization(int id, [FromBody] MoveOrganizationDto dto)
    {
        try
        {
            await _organizationService.MoveOrganizationAsync(id, dto.NewParentId);
            return Ok(new { message = "Organizasyon taşındı" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Organization {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot move organization {Id} to parent {ParentId}", id, dto.NewParentId);
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error moving organization {Id}", id);
            return StatusCode(500, CreateErrorResponse("Organizasyon taşınırken bir hata oluştu", ex));
        }
    }

    /// <summary>
    /// Organizasyonu sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _organizationService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Organization {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Cannot delete organization {Id}", id);
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting organization {Id}", id);
            return StatusCode(500, CreateErrorResponse("Organizasyon silinirken bir hata oluştu", ex));
        }
    }

    // ========== PERSONEL YÖNETİMİ ==========

    /// <summary>
    /// Organizasyondaki personelleri getir (hiyerarşik)
    /// </summary>
    [HttpGet("{id}/personnel")]
    public async Task<IActionResult> GetPersonnel(int id)
    {
        try
        {
            var personnel = await _organizationService.GetPersonnelByOrganizationIdAsync(id);
            return Ok(personnel);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Organization {Id} not found", id);
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading personnel for organization {Id}", id);
            return StatusCode(500, CreateErrorResponse("Personeller yüklenirken bir hata oluştu", ex));
        }
    }

    /// <summary>
    /// Müşteriye ait tüm personel havuzunu getir (autocomplete için)
    /// </summary>
    [HttpGet("personnel-pool/{customerId}")]
    public async Task<IActionResult> GetPersonnelPool(int customerId)
    {
        try
        {
            var personnel = await _organizationService.GetPersonnelPoolAsync(customerId);
            return Ok(personnel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading personnel pool for customer {CustomerId}", customerId);
            return StatusCode(500, CreateErrorResponse("Personel havuzu yüklenirken bir hata oluştu", ex));
        }
    }

    /// <summary>
    /// Personeli organizasyona ata
    /// </summary>
    [HttpPost("assign-personnel")]
    public async Task<IActionResult> AssignPersonnel([FromBody] AssignPersonnelToOrganizationDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _organizationService.AssignPersonnelToOrganizationAsync(dto);
            return Ok(new { message = "Personel organizasyona atandı" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personnel or organization not found");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while assigning personnel");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning personnel to organization");
            return StatusCode(500, CreateErrorResponse("Personel atanırken bir hata oluştu", ex));
        }
    }

    /// <summary>
    /// Personeli organizasyondan çıkar
    /// </summary>
    [HttpDelete("{organizationId}/personnel/{personnelId}")]
    public async Task<IActionResult> RemovePersonnel(int organizationId, int personnelId)
    {
        try
        {
            await _organizationService.RemovePersonnelFromOrganizationAsync(personnelId, organizationId);
            return Ok(new { message = "Personel organizasyondan çıkarıldı" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personnel {PersonnelId} not found", personnelId);
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while removing personnel");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing personnel {PersonnelId} from organization {OrganizationId}", personnelId, organizationId);
            return StatusCode(500, CreateErrorResponse("Personel çıkarılırken bir hata oluştu", ex));
        }
    }

    /// <summary>
    /// Personelin süpervizörünü ayarla
    /// </summary>
    [HttpPut("personnel/{personnelId}/supervisor")]
    public async Task<IActionResult> SetSupervisor(int personnelId, [FromBody] int? supervisorId)
    {
        try
        {
            await _organizationService.SetSupervisorAsync(personnelId, supervisorId);
            return Ok(new { message = "Süpervizör atandı" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personnel or supervisor not found");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while setting supervisor");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting supervisor for personnel {PersonnelId}", personnelId);
            return StatusCode(500, CreateErrorResponse("Süpervizör atanırken bir hata oluştu", ex));
        }
    }

    /// <summary>
    /// Ekip üyelerini yeni süpervizöre devret ve personeli organizasyondan çıkar
    /// </summary>
    [HttpPost("{organizationId}/transfer-and-remove")]
    public async Task<IActionResult> TransferAndRemove(int organizationId, [FromBody] TransferAndRemoveDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            await _organizationService.TransferTeamAndRemoveAsync(organizationId, dto.PersonnelIdToRemove, dto.NewSupervisorId);
            return Ok(new { message = "Ekip üyeleri devredildi ve personel organizasyondan çıkarıldı" });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Personnel or organization not found");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Validation error while transferring team");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error transferring team and removing personnel");
            return StatusCode(500, CreateErrorResponse("Transfer işlemi sırasında bir hata oluştu", ex));
        }
    }
}

public class TransferAndRemoveDto
{
    public int PersonnelIdToRemove { get; set; }
    public int NewSupervisorId { get; set; }
}

public class MoveOrganizationDto
{
    public int? NewParentId { get; set; }
}
