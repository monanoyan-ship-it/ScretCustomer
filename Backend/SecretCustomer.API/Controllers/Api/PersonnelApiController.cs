using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Personnel;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/personnel")]
[Authorize(Roles = "Admin,TeamLeader")]
public class PersonnelApiController : ControllerBase
{
    private readonly IPersonnelService _personnelService;
    private readonly ILogger<PersonnelApiController> _logger;

    public PersonnelApiController(IPersonnelService personnelService, ILogger<PersonnelApiController> logger)
    {
        _personnelService = personnelService;
        _logger = logger;
    }

    /// <summary>
    /// Personel listesi (sayfalı, filtreli)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PersonnelFilterDto filter)
    {
        try
        {
            var result = await _personnelService.GetAllAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personnel list");
            return StatusCode(500, new { message = "Personel listesi yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Personel detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var personnel = await _personnelService.GetByIdAsync(id);
            if (personnel == null)
                return NotFound(new { message = "Personel bulunamadı." });

            return Ok(personnel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personnel {Id}", id);
            return StatusCode(500, new { message = "Personel bilgileri yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Yeni personel oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePersonnelDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var personnel = await _personnelService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = personnel.Id }, personnel);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating personnel");
            return StatusCode(500, new { message = "Personel oluşturulurken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Personel güncelle
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdatePersonnelDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var personnel = await _personnelService.UpdateAsync(id, dto);
            if (personnel == null)
                return NotFound(new { message = "Personel bulunamadı." });

            return Ok(personnel);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating personnel {Id}", id);
            return StatusCode(500, new { message = "Personel güncellenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Personel sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var result = await _personnelService.DeleteAsync(id);
            if (!result)
                return NotFound(new { message = "Personel bulunamadı." });

            return Ok(new { message = "Personel başarıyla silindi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting personnel {Id}", id);
            return StatusCode(500, new { message = "Personel silinirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Şubeye göre personel listesi
    /// </summary>
    [HttpGet("branch/{branchId}")]
    public async Task<IActionResult> GetByBranch(Guid branchId)
    {
        try
        {
            var personnel = await _personnelService.GetByBranchAsync(branchId);
            return Ok(personnel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personnel by branch {BranchId}", branchId);
            return StatusCode(500, new { message = "Personel listesi yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Müşteriye göre personel listesi
    /// </summary>
    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(Guid customerId)
    {
        try
        {
            var personnel = await _personnelService.GetByCustomerAsync(customerId);
            return Ok(personnel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personnel by customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Personel listesi yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// TC Kimlik No kontrolü
    /// </summary>
    [HttpGet("check-tc/{tcKimlikNo}")]
    public async Task<IActionResult> CheckTcKimlikNo(string tcKimlikNo, [FromQuery] Guid? excludeId)
    {
        try
        {
            var exists = await _personnelService.ExistsByTcKimlikNoAsync(tcKimlikNo, excludeId);
            return Ok(new { exists });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking TC Kimlik No");
            return StatusCode(500, new { message = "Kontrol sırasında bir hata oluştu." });
        }
    }
}
