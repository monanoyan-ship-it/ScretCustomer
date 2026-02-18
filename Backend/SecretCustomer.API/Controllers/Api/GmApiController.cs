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
    // DÖNEM SORU
    // =============================================

    [HttpGet("donem-sorular")]
    public async Task<IActionResult> GetDonemSorular([FromQuery] int? customerId, [FromQuery] int? hedefFirmaId, [FromQuery] int? donemId)
    {
        try
        {
            var result = await _gmService.GetDonemSorularAsync(customerId, hedefFirmaId, donemId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem soruları yüklenirken hata");
            return StatusCode(500, CreateErrorResponse("Dönem soruları yüklenirken hata oluştu", ex));
        }
    }

    [HttpPost("donemler/{donemId}/soru")]
    public async Task<IActionResult> CreateDonemSoru(int donemId, [FromBody] CreateDonemSoruRequest request)
    {
        try
        {
            var result = await _gmService.CreateDonemSoruAsync(donemId, request);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem soru eklenirken hata");
            return StatusCode(500, CreateErrorResponse("Soru eklenirken hata oluştu", ex));
        }
    }

    [HttpPut("donem-soru/{id}")]
    public async Task<IActionResult> UpdateDonemSoru(int id, [FromBody] UpdateDonemSoruRequest request)
    {
        try
        {
            var result = await _gmService.UpdateDonemSoruAsync(id, request);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem soru güncellenirken hata");
            return StatusCode(500, CreateErrorResponse("Soru güncellenirken hata oluştu", ex));
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

    [HttpPost("donemler/{donemId}/sorular/import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportDonemSorular(int donemId, [FromForm] int customerId, [FromForm] int hedefFirmaId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { message = "Sadece Excel dosyaları (.xlsx, .xls) kabul edilir." });

            using var stream = file.OpenReadStream();
            var (imported, skipped, errors) = await _gmService.ImportDonemSorularFromExcelAsync(donemId, customerId, hedefFirmaId, stream);

            return Ok(new { imported, skipped, errors, message = $"{imported} soru eklendi, {skipped} satır atlandı." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem soru Excel import hatası");
            return StatusCode(500, CreateErrorResponse("Excel import sırasında hata oluştu", ex));
        }
    }

    [HttpPost("donemler/{donemId}/sorular/import-with-matching")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportDonemSorularWithMatching(int donemId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { message = "Sadece Excel dosyaları (.xlsx, .xls) kabul edilir." });

            using var stream = file.OpenReadStream();
            var result = await _gmService.ImportDonemSorularWithMatchingAsync(donemId, stream);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem soru Excel import (eşleştirmeli) hatası");
            return StatusCode(500, CreateErrorResponse("Excel import sırasında hata oluştu", ex));
        }
    }

    [HttpPost("donemler/{donemId}/sorular/save-unmatched")]
    public async Task<IActionResult> SaveUnmatchedSorular(int donemId, [FromBody] List<SaveUnmatchedSoruItem> items)
    {
        try
        {
            var saved = await _gmService.SaveUnmatchedSorularAsync(donemId, items);
            return Ok(new { saved, message = $"{saved} soru kaydedildi." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Eşleşmeyen sorular kaydedilirken hata");
            return StatusCode(500, CreateErrorResponse("Kaydetme sırasında hata oluştu", ex));
        }
    }

    // =============================================
    // DÖNEM
    // =============================================

    [HttpGet("donemler")]
    public async Task<IActionResult> GetDonemler()
    {
        try
        {
            var result = await _gmService.GetDonemlerAsync();
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

    [HttpPost("donemler/{donemId}/kopyala")]
    public async Task<IActionResult> CopyDonem(int donemId, [FromBody] CopyDonemRequest request)
    {
        try
        {
            var userId = GetUserId();
            var newId = await _gmService.CopyDonemAsync(donemId, request.Ad, request.BaslangicTarihi, request.BitisTarihi, userId);
            return Ok(new { id = newId, message = "Dönem kopyalandı." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Dönem kopyalanırken hata");
            return StatusCode(500, CreateErrorResponse("Dönem kopyalanırken hata oluştu", ex));
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

    // =============================================
    // KUPONLU SORU IMPORT (AKTİF DÖNEM)
    // =============================================

    [HttpPost("donemler/{donemId}/kuponlu-sorular/import")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportKuponluSorular(int donemId, [FromForm] int customerId, [FromForm] int hedefFirmaId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { message = "Sadece Excel dosyaları (.xlsx, .xls) kabul edilir." });

            using var stream = file.OpenReadStream();
            var (imported, skipped, errors) = await _gmService.ImportKuponluSorularFromExcelAsync(donemId, customerId, hedefFirmaId, stream);

            return Ok(new { imported, skipped, errors, message = $"{imported} kuponlu soru eklendi, {skipped} satır atlandı." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kuponlu soru Excel import hatası");
            return StatusCode(500, CreateErrorResponse("Kuponlu Excel import sırasında hata oluştu", ex));
        }
    }

    [HttpPost("donemler/{donemId}/kuponlu-sorular/import-with-matching")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ImportKuponluSorularWithMatching(int donemId, IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "Dosya seçilmedi." });

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new { message = "Sadece Excel dosyaları (.xlsx, .xls) kabul edilir." });

            using var stream = file.OpenReadStream();
            var result = await _gmService.ImportKuponluSorularWithMatchingAsync(donemId, stream);

            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kuponlu soru Excel import (eşleştirmeli) hatası");
            return StatusCode(500, CreateErrorResponse("Kuponlu Excel import sırasında hata oluştu", ex));
        }
    }

    [HttpPost("donemler/{donemId}/kuponlu-sorular/save-unmatched")]
    public async Task<IActionResult> SaveUnmatchedKuponluSorular(int donemId, [FromBody] List<SaveUnmatchedSoruItem> items)
    {
        try
        {
            var saved = await _gmService.SaveUnmatchedKuponluSorularAsync(donemId, items);
            return Ok(new { saved, message = $"{saved} kuponlu soru kaydedildi." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kuponlu eşleşmeyen sorular kaydedilirken hata");
            return StatusCode(500, CreateErrorResponse("Kaydetme sırasında hata oluştu", ex));
        }
    }

    [HttpPost("donemler/{donemId}/kuponlu-dagit")]
    public async Task<IActionResult> KuponluDagit(int donemId)
    {
        try
        {
            var atamaCount = await _gmService.KuponluDagitAsync(donemId);
            return Ok(new { success = true, atamaCount, message = $"{atamaCount} kuponlu atama oluşturuldu." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kuponlu dağıtım yapılırken hata");
            return StatusCode(500, CreateErrorResponse("Kuponlu dağıtım yapılırken hata oluştu", ex));
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

public class CopyDonemRequest
{
    public string Ad { get; set; } = string.Empty;
    public DateTime BaslangicTarihi { get; set; }
    public DateTime BitisTarihi { get; set; }
}
