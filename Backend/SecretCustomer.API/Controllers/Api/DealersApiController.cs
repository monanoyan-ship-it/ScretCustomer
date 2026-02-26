using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Dealer;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/dealers")]
[Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
public class DealersApiController : BaseApiController
{
    private readonly IDealerService _dealerService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILocalizationService _localizationService;

    public DealersApiController(
        IDealerService dealerService,
        IAuditLogService auditLogService,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _dealerService = dealerService;
        _auditLogService = auditLogService;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Bayi listesi (sayfalı, filtreli)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DealerFilterDto filter)
    {
        try
        {
            var result = await _dealerService.GetAllAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting dealer list", "Dealers", ex);
            return StatusCode(500, CreateErrorResponse("Bayi listesi yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Bayi detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var dealer = await _dealerService.GetByIdAsync(id);
            if (dealer == null)
                return NotFound(CreateErrorResponse("Bayi bulunamadı"));

            return Ok(dealer);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting dealer {id}", "Dealers", ex);
            return StatusCode(500, CreateErrorResponse("Bayi bilgisi yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Yeni bayi oluştur
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDealerDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dealer = await _dealerService.CreateAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id = dealer.Id }, dealer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error creating dealer", "Dealers", ex);
            return StatusCode(500, CreateErrorResponse("Bayi oluşturulurken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Bayi güncelle
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateDealerDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var dealer = await _dealerService.UpdateAsync(id, dto);
            if (dealer == null)
                return NotFound(CreateErrorResponse("Bayi bulunamadı"));

            return Ok(dealer);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(CreateErrorResponse(ex.Message, ex));
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating dealer {id}", "Dealers", ex);
            return StatusCode(500, CreateErrorResponse("Bayi güncellenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Bayi sil
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            var result = await _dealerService.DeleteAsync(id);
            if (!result)
                return NotFound(CreateErrorResponse("Bayi bulunamadı"));

            return Ok(new { message = "Bayi başarıyla silindi" });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error deleting dealer {id}", "Dealers", ex);
            return StatusCode(500, CreateErrorResponse("Bayi silinirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Müşteriye göre bayi listesi
    /// </summary>
    [HttpGet("customer/{customerId}")]
    public async Task<IActionResult> GetByCustomer(int customerId)
    {
        try
        {
            var dealers = await _dealerService.GetByCustomerAsync(customerId);
            return Ok(dealers);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting dealers by customer {customerId}", "Dealers", ex);
            return StatusCode(500, CreateErrorResponse("Bayi listesi yüklenirken hata oluştu", ex));
        }
    }

    /// <summary>
    /// Yeni bayi kodu üret
    /// </summary>
    [HttpGet("generate-code/{customerId}")]
    public async Task<IActionResult> GenerateCode(int customerId)
    {
        try
        {
            var code = await _dealerService.GenerateCodeAsync(customerId);
            return Ok(new { code });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error generating dealer code for customer {customerId}", "Dealers", ex);
            return StatusCode(500, CreateErrorResponse("Bayi kodu üretilirken hata oluştu", ex));
        }
    }
}
