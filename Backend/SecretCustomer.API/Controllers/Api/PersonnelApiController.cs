using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Personnel;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/personnel")]
[Authorize(Roles = "Admin,TeamLeader")]
public class PersonnelApiController : BaseApiController
{
    private readonly IPersonnelService _personnelService;
    private readonly ILogger<PersonnelApiController> _logger;
    private readonly ILocalizationService _localizationService;

    public PersonnelApiController(
        IPersonnelService personnelService,
        ILogger<PersonnelApiController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _personnelService = personnelService;
        _logger = logger;
        _localizationService = localizationService;
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.LoadListError"), ex));
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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.NotFound")));

            return Ok(personnel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting personnel {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.LoadError"), ex));
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
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating personnel");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.CreateError"), ex));
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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.NotFound")));

            return Ok(personnel);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating personnel {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.UpdateError"), ex));
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
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.NotFound")));

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Personnel.DeleteSuccess") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting personnel {Id}", id);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.DeleteError"), ex));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.LoadListError"), ex));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.LoadListError"), ex));
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
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Personnel.CheckError"), ex));
        }
    }
}
