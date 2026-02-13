using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.GolgeMusteri;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/gm")]
[Authorize(Roles = "Admin")]
public class GmApiController : BaseApiController
{
    private readonly IGmService _gmService;
    private readonly ILogger<GmApiController> _logger;

    public GmApiController(
        IGmService gmService,
        ILogger<GmApiController> logger,
        IConfiguration configuration) : base(configuration)
    {
        _gmService = gmService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var claim = User.FindFirst("UserId")?.Value
            ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    // =============================================
    // HEDEF FIRMA
    // =============================================

    [HttpGet("hedef-firmalar")]
    public async Task<IActionResult> GetHedefFirmalar([FromQuery] int? customerId)
    {
        try
        {
            var result = await _gmService.GetHedefFirmalarAsync(customerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hedef firmalar yüklenirken hata");
            return StatusCode(500, CreateErrorResponse("Hedef firmalar yüklenirken hata oluştu", ex));
        }
    }

    [HttpGet("hedef-firmalar/{id}")]
    public async Task<IActionResult> GetHedefFirma(int id)
    {
        try
        {
            var result = await _gmService.GetHedefFirmaByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hedef firma detayı yüklenirken hata");
            return StatusCode(500, CreateErrorResponse("Hedef firma detayı yüklenirken hata oluştu", ex));
        }
    }

    [HttpPost("hedef-firmalar")]
    public async Task<IActionResult> CreateHedefFirma([FromBody] CreateGmHedefFirmaDto dto)
    {
        try
        {
            var result = await _gmService.CreateHedefFirmaAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hedef firma oluşturulurken hata");
            return StatusCode(500, CreateErrorResponse("Hedef firma oluşturulurken hata oluştu", ex));
        }
    }

    [HttpPut("hedef-firmalar/{id}")]
    public async Task<IActionResult> UpdateHedefFirma(int id, [FromBody] UpdateGmHedefFirmaDto dto)
    {
        try
        {
            var result = await _gmService.UpdateHedefFirmaAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hedef firma güncellenirken hata");
            return StatusCode(500, CreateErrorResponse("Hedef firma güncellenirken hata oluştu", ex));
        }
    }

    [HttpDelete("hedef-firmalar/{id}")]
    public async Task<IActionResult> DeleteHedefFirma(int id)
    {
        try
        {
            var result = await _gmService.DeleteHedefFirmaAsync(id);
            if (!result) return NotFound();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hedef firma silinirken hata");
            return StatusCode(500, CreateErrorResponse("Hedef firma silinirken hata oluştu", ex));
        }
    }

    // =============================================
    // SORU
    // =============================================

    [HttpGet("sorular")]
    public async Task<IActionResult> GetSorular([FromQuery] int? customerId, [FromQuery] int? hedefFirmaId)
    {
        try
        {
            var result = await _gmService.GetSorularAsync(customerId, hedefFirmaId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sorular yüklenirken hata");
            return StatusCode(500, CreateErrorResponse("Sorular yüklenirken hata oluştu", ex));
        }
    }

    [HttpGet("sorular/{id}")]
    public async Task<IActionResult> GetSoru(int id)
    {
        try
        {
            var result = await _gmService.GetSoruByIdAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Soru detayı yüklenirken hata");
            return StatusCode(500, CreateErrorResponse("Soru detayı yüklenirken hata oluştu", ex));
        }
    }

    [HttpPost("sorular")]
    public async Task<IActionResult> CreateSoru([FromBody] CreateGmSoruDto dto)
    {
        try
        {
            var result = await _gmService.CreateSoruAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Soru oluşturulurken hata");
            return StatusCode(500, CreateErrorResponse("Soru oluşturulurken hata oluştu", ex));
        }
    }

    [HttpPut("sorular/{id}")]
    public async Task<IActionResult> UpdateSoru(int id, [FromBody] UpdateGmSoruDto dto)
    {
        try
        {
            var result = await _gmService.UpdateSoruAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Soru güncellenirken hata");
            return StatusCode(500, CreateErrorResponse("Soru güncellenirken hata oluştu", ex));
        }
    }

    [HttpDelete("sorular/{id}")]
    public async Task<IActionResult> DeleteSoru(int id)
    {
        try
        {
            var result = await _gmService.DeleteSoruAsync(id);
            if (!result) return NotFound();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Soru silinirken hata");
            return StatusCode(500, CreateErrorResponse("Soru silinirken hata oluştu", ex));
        }
    }

    // =============================================
    // DÖNEM
    // =============================================

    [HttpGet("donemler")]
    public async Task<IActionResult> GetDonemler([FromQuery] int? customerId)
    {
        try
        {
            var result = await _gmService.GetDonemlerAsync(customerId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönemler yüklenirken hata");
            return StatusCode(500, CreateErrorResponse("Dönemler yüklenirken hata oluştu", ex));
        }
    }

    [HttpGet("donemler/{id}")]
    public async Task<IActionResult> GetDonemDetail(int id)
    {
        try
        {
            var result = await _gmService.GetDonemDetailAsync(id);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem detayı yüklenirken hata");
            return StatusCode(500, CreateErrorResponse("Dönem detayı yüklenirken hata oluştu", ex));
        }
    }

    [HttpPost("donemler")]
    public async Task<IActionResult> CreateDonem([FromBody] CreateGmDonemDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _gmService.CreateDonemAsync(dto, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem oluşturulurken hata");
            return StatusCode(500, CreateErrorResponse("Dönem oluşturulurken hata oluştu", ex));
        }
    }

    [HttpPut("donemler/{id}")]
    public async Task<IActionResult> UpdateDonem(int id, [FromBody] UpdateGmDonemDto dto)
    {
        try
        {
            var result = await _gmService.UpdateDonemAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem güncellenirken hata");
            return StatusCode(500, CreateErrorResponse("Dönem güncellenirken hata oluştu", ex));
        }
    }

    [HttpDelete("donemler/{id}")]
    public async Task<IActionResult> DeleteDonem(int id)
    {
        try
        {
            var result = await _gmService.DeleteDonemAsync(id);
            if (!result) return NotFound();
            return Ok(new { success = true });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem silinirken hata");
            return StatusCode(500, CreateErrorResponse("Dönem silinirken hata oluştu", ex));
        }
    }

    // =============================================
    // DÖNEM ALT YÖNETİM
    // =============================================

    [HttpPost("donemler/{donemId}/personel")]
    public async Task<IActionResult> AddDonemPersonel(int donemId, [FromBody] AddDonemPersonelRequest request)
    {
        try
        {
            var result = await _gmService.AddDonemPersonelAsync(donemId, request.UserId);
            if (!result) return BadRequest(new { message = "Personel eklenemedi. Dönem taslak olmayabilir veya personel zaten ekli." });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem personel eklenirken hata");
            return StatusCode(500, CreateErrorResponse("Personel eklenirken hata oluştu", ex));
        }
    }

    [HttpDelete("donem-personel/{donemPersonelId}")]
    public async Task<IActionResult> RemoveDonemPersonel(int donemPersonelId)
    {
        try
        {
            var result = await _gmService.RemoveDonemPersonelAsync(donemPersonelId);
            if (!result) return BadRequest(new { message = "Personel çıkarılamadı." });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem personel çıkarılırken hata");
            return StatusCode(500, CreateErrorResponse("Personel çıkarılırken hata oluştu", ex));
        }
    }

    [HttpPost("donemler/{donemId}/soru")]
    public async Task<IActionResult> AddDonemSoru(int donemId, [FromBody] AddDonemSoruRequest request)
    {
        try
        {
            var result = await _gmService.AddDonemSoruAsync(donemId, request.SoruId, request.AranmaSayisi);
            if (!result) return BadRequest(new { message = "Soru eklenemedi. Dönem taslak olmayabilir veya soru zaten ekli." });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem soru eklenirken hata");
            return StatusCode(500, CreateErrorResponse("Soru eklenirken hata oluştu", ex));
        }
    }

    [HttpDelete("donem-soru/{donemSoruId}")]
    public async Task<IActionResult> RemoveDonemSoru(int donemSoruId)
    {
        try
        {
            var result = await _gmService.RemoveDonemSoruAsync(donemSoruId);
            if (!result) return BadRequest(new { message = "Soru çıkarılamadı." });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem soru çıkarılırken hata");
            return StatusCode(500, CreateErrorResponse("Soru çıkarılırken hata oluştu", ex));
        }
    }

    [HttpPut("donem-soru/{donemSoruId}")]
    public async Task<IActionResult> UpdateDonemSoru(int donemSoruId, [FromBody] UpdateDonemSoruRequest request)
    {
        try
        {
            var result = await _gmService.UpdateDonemSoruAsync(donemSoruId, request.AranmaSayisi);
            if (!result) return BadRequest(new { message = "Soru güncellenemedi." });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem soru güncellenirken hata");
            return StatusCode(500, CreateErrorResponse("Soru güncellenirken hata oluştu", ex));
        }
    }

    [HttpPost("donemler/{donemId}/kupon")]
    public async Task<IActionResult> AddDonemKupon(int donemId, [FromBody] AddDonemKuponRequest request)
    {
        try
        {
            var result = await _gmService.AddDonemKuponAsync(donemId, request.KuponKodu);
            if (!result) return BadRequest(new { message = "Kupon eklenemedi." });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem kupon eklenirken hata");
            return StatusCode(500, CreateErrorResponse("Kupon eklenirken hata oluştu", ex));
        }
    }

    [HttpPost("donemler/{donemId}/kuponlar")]
    public async Task<IActionResult> AddDonemKuponlar(int donemId, [FromBody] AddDonemKuponlarRequest request)
    {
        try
        {
            var results = await _gmService.AddDonemKuponlarAsync(donemId, request.KuponKodlari);
            return Ok(new { success = true, results });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem kuponlar eklenirken hata");
            return StatusCode(500, CreateErrorResponse("Kuponlar eklenirken hata oluştu", ex));
        }
    }

    [HttpDelete("donem-kupon/{donemKuponId}")]
    public async Task<IActionResult> RemoveDonemKupon(int donemKuponId)
    {
        try
        {
            var result = await _gmService.RemoveDonemKuponAsync(donemKuponId);
            if (!result) return BadRequest(new { message = "Kupon çıkarılamadı." });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem kupon çıkarılırken hata");
            return StatusCode(500, CreateErrorResponse("Kupon çıkarılırken hata oluştu", ex));
        }
    }

    // =============================================
    // AKTİF ET & TAMAMLA
    // =============================================

    [HttpPost("donemler/{donemId}/aktif-et")]
    public async Task<IActionResult> AktifEt(int donemId)
    {
        try
        {
            var atamaCount = await _gmService.AktifEtAsync(donemId);
            return Ok(new { success = true, atamaCount, message = $"{atamaCount} atama oluşturuldu." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem aktif edilirken hata");
            return StatusCode(500, CreateErrorResponse("Dönem aktif edilirken hata oluştu", ex));
        }
    }

    [HttpPost("donemler/{donemId}/tamamla")]
    public async Task<IActionResult> Tamamla(int donemId)
    {
        try
        {
            var result = await _gmService.TamamlaAsync(donemId);
            if (!result) return BadRequest(new { message = "Dönem tamamlanamadı. Dönem aktif olmayabilir." });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem tamamlanırken hata");
            return StatusCode(500, CreateErrorResponse("Dönem tamamlanırken hata oluştu", ex));
        }
    }

    // =============================================
    // TAKİP
    // =============================================

    [HttpGet("atamalar")]
    public async Task<IActionResult> GetAtamalar([FromQuery] int donemId, [FromQuery] int? userId, [FromQuery] int? durumId)
    {
        try
        {
            var result = await _gmService.GetAtamalarAsync(donemId, userId, durumId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Atamalar yüklenirken hata");
            return StatusCode(500, CreateErrorResponse("Atamalar yüklenirken hata oluştu", ex));
        }
    }
}

// Request DTOs (controller-specific)
public class AddDonemPersonelRequest
{
    public int UserId { get; set; }
}

public class AddDonemSoruRequest
{
    public int SoruId { get; set; }
    public int AranmaSayisi { get; set; } = 1;
}

public class UpdateDonemSoruRequest
{
    public int AranmaSayisi { get; set; }
}

public class AddDonemKuponRequest
{
    public string KuponKodu { get; set; } = string.Empty;
}

public class AddDonemKuponlarRequest
{
    public List<string> KuponKodlari { get; set; } = new();
}
