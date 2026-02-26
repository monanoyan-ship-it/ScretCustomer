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
    private readonly IAuditLogService _auditLogService;
    private readonly ILocalizationService _localizationService;

    public CustomerOrganizationsApiController(
        ICustomerOrganizationService organizationService,
        IAuditLogService auditLogService,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _organizationService = organizationService;
        _auditLogService = auditLogService;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Tüm organizasyonları getir (filtreli ve sayfalı)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] OrganizationFilterDto filter)
    {
        try
        {
            var result = await _organizationService.GetFilteredListAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error loading organizations with filters", "CustomerOrganizations", ex);
            return StatusCode(500, CreateErrorResponse("Organizasyonlar yüklenirken bir hata oluştu", ex));
        }
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
            await _auditLogService.LogErrorAsync($"Error loading organizations for customer {customerId}", "CustomerOrganizations", ex);
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
            await _auditLogService.LogErrorAsync($"Error loading organization tree for customer {customerId}", "CustomerOrganizations", ex);
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
            await _auditLogService.LogErrorAsync($"Error loading organization {id}", "CustomerOrganizations", ex);
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
            await _auditLogService.LogWarningAsync($"Customer or parent organization not found: {ex.Message}", "CustomerOrganizations");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogWarningAsync($"Validation error while creating organization: {ex.Message}", "CustomerOrganizations");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error creating organization", "CustomerOrganizations", ex);
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
            await _auditLogService.LogWarningAsync($"Organization {id} not found: {ex.Message}", "CustomerOrganizations");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogWarningAsync($"Validation error while updating organization {id}: {ex.Message}", "CustomerOrganizations");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating organization {id}", "CustomerOrganizations", ex);
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
            await _auditLogService.LogWarningAsync($"Organization {id} not found: {ex.Message}", "CustomerOrganizations");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogWarningAsync($"Cannot move organization {id} to parent {dto.NewParentId}: {ex.Message}", "CustomerOrganizations");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error moving organization {id}", "CustomerOrganizations", ex);
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
            await _auditLogService.LogWarningAsync($"Organization {id} not found: {ex.Message}", "CustomerOrganizations");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogWarningAsync($"Cannot delete organization {id}: {ex.Message}", "CustomerOrganizations");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error deleting organization {id}", "CustomerOrganizations", ex);
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
            await _auditLogService.LogWarningAsync($"Organization {id} not found: {ex.Message}", "CustomerOrganizations");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading personnel for organization {id}", "CustomerOrganizations", ex);
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
            await _auditLogService.LogErrorAsync($"Error loading personnel pool for customer {customerId}", "CustomerOrganizations", ex);
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
            await _auditLogService.LogWarningAsync($"Personnel or organization not found: {ex.Message}", "CustomerOrganizations");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogWarningAsync($"Validation error while assigning personnel: {ex.Message}", "CustomerOrganizations");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error assigning personnel to organization", "CustomerOrganizations", ex);
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
            await _auditLogService.LogWarningAsync($"Personnel {personnelId} not found: {ex.Message}", "CustomerOrganizations");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogWarningAsync($"Validation error while removing personnel: {ex.Message}", "CustomerOrganizations");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error removing personnel {personnelId} from organization {organizationId}", "CustomerOrganizations", ex);
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
            await _auditLogService.LogWarningAsync($"Personnel or supervisor not found: {ex.Message}", "CustomerOrganizations");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogWarningAsync($"Validation error while setting supervisor: {ex.Message}", "CustomerOrganizations");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error setting supervisor for personnel {personnelId}", "CustomerOrganizations", ex);
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
            await _auditLogService.LogWarningAsync($"Personnel or organization not found: {ex.Message}", "CustomerOrganizations");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogWarningAsync($"Validation error while transferring team: {ex.Message}", "CustomerOrganizations");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error transferring team and removing personnel", "CustomerOrganizations", ex);
            return StatusCode(500, CreateErrorResponse("Transfer işlemi sırasında bir hata oluştu", ex));
        }
    }

    // ========== COKLU ORGANIZASYON DESTEGI (Junction Table) ==========

    /// <summary>
    /// Personelin gorev aldigi tum organizasyonlari getir
    /// </summary>
    [HttpGet("~/api/customer-personnel/{personnelId}/organizations")]
    public async Task<IActionResult> GetPersonnelOrganizations(int personnelId)
    {
        try
        {
            var assignments = await _organizationService.GetPersonnelOrganizationsAsync(personnelId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading organizations for personnel {personnelId}", "CustomerOrganizations", ex);
            return StatusCode(500, CreateErrorResponse("Organizasyonlar yuklenirken bir hata olustu", ex));
        }
    }

    /// <summary>
    /// Organizasyondaki tum personel atamalarini getir (junction table)
    /// </summary>
    [HttpGet("{organizationId}/assignments")]
    public async Task<IActionResult> GetOrganizationAssignments(int organizationId)
    {
        try
        {
            var assignments = await _organizationService.GetOrganizationPersonnelAssignmentsAsync(organizationId);
            return Ok(assignments);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error loading assignments for organization {organizationId}", "CustomerOrganizations", ex);
            return StatusCode(500, CreateErrorResponse("Atamalar yuklenirken bir hata olustu", ex));
        }
    }

    /// <summary>
    /// Personeli organizasyona ata (coklu organizasyon destegi)
    /// </summary>
    [HttpPost("{organizationId}/personnel/{personnelId}")]
    public async Task<IActionResult> AddPersonnelToOrganization(
        int organizationId,
        int personnelId,
        [FromBody] AddPersonnelToOrganizationRequestDto? dto = null)
    {
        try
        {
            var assignment = await _organizationService.AddPersonnelToOrganizationAsync(
                personnelId,
                organizationId,
                dto?.SupervisorId,
                dto?.Notes);
            return Ok(assignment);
        }
        catch (KeyNotFoundException ex)
        {
            await _auditLogService.LogWarningAsync($"Personnel or organization not found: {ex.Message}", "CustomerOrganizations");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogWarningAsync($"Validation error while adding personnel to organization: {ex.Message}", "CustomerOrganizations");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error adding personnel {personnelId} to organization {organizationId}", "CustomerOrganizations", ex);
            return StatusCode(500, CreateErrorResponse("Personel atanirken bir hata olustu", ex));
        }
    }

    /// <summary>
    /// Personelin organizasyondaki atamasini guncelle (supervisor degistir vb.)
    /// </summary>
    [HttpPut("{organizationId}/personnel/{personnelId}")]
    public async Task<IActionResult> UpdatePersonnelOrganizationAssignment(
        int organizationId,
        int personnelId,
        [FromBody] UpdatePersonnelOrganizationAssignmentDto dto)
    {
        try
        {
            var assignment = await _organizationService.UpdatePersonnelOrganizationAssignmentAsync(
                personnelId,
                organizationId,
                dto);
            return Ok(assignment);
        }
        catch (KeyNotFoundException ex)
        {
            await _auditLogService.LogWarningAsync($"Assignment not found: {ex.Message}", "CustomerOrganizations");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            await _auditLogService.LogWarningAsync($"Validation error while updating assignment: {ex.Message}", "CustomerOrganizations");
            return BadRequest(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating assignment for personnel {personnelId} in organization {organizationId}", "CustomerOrganizations", ex);
            return StatusCode(500, CreateErrorResponse("Atama guncellenirken bir hata olustu", ex));
        }
    }

    /// <summary>
    /// Personeli organizasyondan cikar (coklu organizasyon destegi)
    /// </summary>
    [HttpDelete("{organizationId}/personnel/{personnelId}/v2")]
    public async Task<IActionResult> RemovePersonnelFromOrganizationV2(int organizationId, int personnelId)
    {
        try
        {
            await _organizationService.RemovePersonnelFromOrganizationV2Async(personnelId, organizationId);
            return Ok(new { message = "Personel organizasyondan cikarildi" });
        }
        catch (KeyNotFoundException ex)
        {
            await _auditLogService.LogWarningAsync($"Assignment not found: {ex.Message}", "CustomerOrganizations");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error removing personnel {personnelId} from organization {organizationId}", "CustomerOrganizations", ex);
            return StatusCode(500, CreateErrorResponse("Personel cikarilirken bir hata olustu", ex));
        }
    }

    /// <summary>
    /// Personeli belirli bir supervizorün ekibinden cikar
    /// </summary>
    [HttpDelete("{organizationId}/personnel/{personnelId}/supervisor/{supervisorId}")]
    public async Task<IActionResult> RemoveFromTeam(int organizationId, int personnelId, int supervisorId)
    {
        try
        {
            await _organizationService.RemoveFromTeamAsync(personnelId, organizationId, supervisorId);
            return Ok(new { message = "Personel ekipten cikarildi" });
        }
        catch (KeyNotFoundException ex)
        {
            await _auditLogService.LogWarningAsync($"Assignment not found for personnel {personnelId} under supervisor {supervisorId}: {ex.Message}", "CustomerOrganizations");
            return NotFound(CreateErrorResponse(ex.Message));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error removing personnel {personnelId} from team of supervisor {supervisorId}", "CustomerOrganizations", ex);
            return StatusCode(500, CreateErrorResponse("Personel ekipten cikarilirken bir hata olustu", ex));
        }
    }
}

public class AddPersonnelToOrganizationRequestDto
{
    public int? SupervisorId { get; set; }
    public string? Notes { get; set; }
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
