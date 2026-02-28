using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.GolgeMusteri;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/gm")]
[Authorize(Roles = "Admin,Inspector")]
public class GmApiController : BaseApiController
{
    private readonly IGmService _gmService;
    private readonly IAuditLogService _auditLogService;

    public GmApiController(
        IGmService gmService,
        IAuditLogService auditLogService,
        IConfiguration configuration) : base(configuration)
    {
        _gmService = gmService;
        _auditLogService = auditLogService;
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

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Hedef firmalar yüklenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Hedef firmalar yüklenirken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Hedef firma detayı yüklenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Hedef firma detayı yüklenirken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Hedef firma oluşturulurken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Hedef firma oluşturulurken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Hedef firma güncellenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Hedef firma güncellenirken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Hedef firma silinirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Hedef firma silinirken hata oluştu", ex));
        }
    }

    // =============================================
    // DÖNEM SORU
    // =============================================

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem soruları yüklenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dönem soruları yüklenirken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem soru eklenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Soru eklenirken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem soru güncellenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Soru güncellenirken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem soru çıkarılırken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Soru çıkarılırken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem soru Excel import hatası", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Excel import sırasında hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem soru Excel import (eşleştirmeli) hatası", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Excel import sırasında hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Eşleşmeyen sorular kaydedilirken hata", "Gm", ex);
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
            await _auditLogService.LogErrorAsync("Dönemler yüklenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dönemler yüklenirken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem detayı yüklenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dönem detayı yüklenirken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem oluşturulurken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dönem oluşturulurken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem güncellenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dönem güncellenirken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem kopyalanırken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dönem kopyalanırken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem silinirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dönem silinirken hata oluştu", ex));
        }
    }

    // =============================================
    // DÖNEM ALT YÖNETİM
    // =============================================

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem personel eklenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Personel eklenirken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem personel çıkarılırken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Personel çıkarılırken hata oluştu", ex));
        }
    }

    // =============================================
    // AKTİF ET & TAMAMLA
    // =============================================

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem aktif edilirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dönem aktif edilirken hata oluştu", ex));
        }
    }

    // =============================================
    // KUPONLU SORU DAĞITIMI (TEK SORU BAZINDA)
    // =============================================

    [Authorize(Roles = "Admin")]
    [HttpPost("donem-sorular/{donemSoruId}/kuponlu-dagit")]
    public async Task<IActionResult> KuponluDagit(int donemSoruId, [FromBody] KuponluDagitRequest request)
    {
        try
        {
            var atama = await _gmService.KuponluDagitAsync(donemSoruId, request);
            return Ok(new { success = true, atama, message = "Kuponlu atama oluşturuldu." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Kuponlu soru dağıtılırken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Kuponlu soru dağıtılırken hata oluştu", ex));
        }
    }

    // =============================================
    // KUPONLU SORU IMPORT (AKTİF DÖNEM)
    // =============================================

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Kuponlu soru Excel import hatası", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Kuponlu Excel import sırasında hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Kuponlu soru Excel import (eşleştirmeli) hatası", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Kuponlu Excel import sırasında hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Kuponlu eşleşmeyen sorular kaydedilirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Kaydetme sırasında hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
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
            await _auditLogService.LogErrorAsync("Dönem tamamlanırken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dönem tamamlanırken hata oluştu", ex));
        }
    }

    // =============================================
    // DİNLEME TAKİP
    // =============================================

    [HttpGet("dinleme-takip")]
    public async Task<IActionResult> GetDinlemeTakip([FromQuery] int donemId, [FromQuery] int? dinleyenUserId, [FromQuery] int? durumId)
    {
        try
        {
            var result = await _gmService.GetDinlemeTakipAsync(donemId, dinleyenUserId, durumId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Dinleme takip yüklenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dinleme takip yüklenirken hata oluştu", ex));
        }
    }

    // =============================================
    // DİNLEME AYAR
    // =============================================

    [Authorize(Roles = "Admin")]
    [HttpGet("donemler/{donemId}/dinleme-ayarlar")]
    public async Task<IActionResult> GetDinlemeAyarlar(int donemId)
    {
        try
        {
            var result = await _gmService.GetDinlemeAyarlarAsync(donemId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Dinleme ayarları yüklenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dinleme ayarları yüklenirken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("donemler/{donemId}/dinleme-ayarlar")]
    public async Task<IActionResult> CreateDinlemeAyar(int donemId, [FromBody] SecretCustomer.Core.DTOs.GolgeMusteri.CreateGmDinlemeAyarDto dto)
    {
        try
        {
            var result = await _gmService.CreateDinlemeAyarAsync(donemId, dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Dinleme ayarı oluşturulurken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dinleme ayarı oluşturulurken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("dinleme-ayar/{id}")]
    public async Task<IActionResult> UpdateDinlemeAyar(int id, [FromBody] SecretCustomer.Core.DTOs.GolgeMusteri.UpdateGmDinlemeAyarDto dto)
    {
        try
        {
            var result = await _gmService.UpdateDinlemeAyarAsync(id, dto);
            if (result == null) return NotFound();
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Dinleme ayarı güncellenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dinleme ayarı güncellenirken hata oluştu", ex));
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("dinleme-ayar/{id}")]
    public async Task<IActionResult> DeleteDinlemeAyar(int id)
    {
        try
        {
            var result = await _gmService.DeleteDinlemeAyarAsync(id);
            if (!result) return NotFound();
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Dinleme ayarı silinirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dinleme ayarı silinirken hata oluştu", ex));
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
            await _auditLogService.LogErrorAsync("Atamalar yüklenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Atamalar yüklenirken hata oluştu", ex));
        }
    }

    [HttpPut("atamalar/{id}")]
    public async Task<IActionResult> UpdateAtama(int id, [FromBody] UpdateGmAtamaDto dto)
    {
        try
        {
            var result = await _gmService.UpdateAtamaAsync(id, dto);
            if (result == null)
                return NotFound(CreateErrorResponse("Atama bulunamadı"));

            await _auditLogService.LogInfoAsync($"GM Atama {id} güncellendi", "Gm");
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Atama güncellenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Atama güncellenirken hata oluştu", ex));
        }
    }

    // =============================================
    // DINLEME POPUP
    // =============================================

    [HttpGet("dinleme/form/{gmAtamaId}")]
    public async Task<IActionResult> GetDinlemeForm(int gmAtamaId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _gmService.GetDinlemeFormAsync(gmAtamaId, userId);
            if (result == null) return NotFound(new { message = "Dinleme formu bulunamadı." });
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Dinleme formu yüklenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dinleme formu yüklenirken hata oluştu", ex));
        }
    }

    [HttpGet("dinleme/form/edit/{dinlemeId}")]
    public async Task<IActionResult> GetDinlemeEditForm(int dinlemeId)
    {
        try
        {
            var result = await _gmService.GetDinlemeEditFormAsync(dinlemeId);
            if (result == null) return NotFound(new { message = "Dinleme bulunamadı." });
            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Dinleme düzenleme formu yüklenirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dinleme düzenleme formu yüklenirken hata oluştu", ex));
        }
    }

    [HttpPost("dinleme/draft")]
    public async Task<IActionResult> SaveDinlemeDraft([FromBody] GmDinlemeSubmitDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _gmService.SaveDinlemeDraftAsync(dto, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Dinleme taslak kaydedilirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dinleme taslak kaydedilirken hata oluştu", ex));
        }
    }

    [HttpPost("dinleme/submit")]
    public async Task<IActionResult> SubmitDinleme([FromBody] GmDinlemeSubmitDto dto)
    {
        try
        {
            var userId = GetUserId();
            var result = await _gmService.SubmitDinlemeAsync(dto, userId);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Dinleme gönderilirken hata", "Gm", ex);
            return StatusCode(500, CreateErrorResponse("Dinleme gönderilirken hata oluştu", ex));
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
