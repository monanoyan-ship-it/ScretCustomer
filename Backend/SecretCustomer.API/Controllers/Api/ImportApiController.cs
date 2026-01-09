using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Import;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/import")]
[ApiController]
[Authorize(Roles = "Admin")]
public class ImportApiController : BaseApiController
{
    private readonly IImportService _importService;

    public ImportApiController(
        IConfiguration configuration,
        IImportService importService) : base(configuration)
    {
        _importService = importService;
    }

    /// <summary>
    /// CSV dosyasından personel verilerini import eder
    /// </summary>
    /// <param name="file">CSV dosyası</param>
    /// <param name="updateExisting">Mevcut kayıtları güncelle (varsayılan: false)</param>
    /// <returns>Import sonucu</returns>
    [HttpPost("personnel")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB limit
    public async Task<ActionResult<ImportResultDto>> ImportPersonnel(
        IFormFile file,
        [FromQuery] bool updateExisting = false)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Dosya seçilmedi." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".csv")
            {
                return BadRequest(new { message = "Sadece CSV dosyaları kabul edilir." });
            }

            using var stream = file.OpenReadStream();
            var result = await _importService.ImportPersonnelFromCsvAsync(stream, updateExisting);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Import işlemi sırasında hata oluştu.", ex));
        }
    }

    /// <summary>
    /// CSV içeriğini doğrudan import eder (test için)
    /// </summary>
    [HttpPost("personnel/raw")]
    public async Task<ActionResult<ImportResultDto>> ImportPersonnelRaw(
        [FromBody] ImportRawRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.CsvContent))
            {
                return BadRequest(new { message = "CSV içeriği boş." });
            }

            var result = await _importService.ImportPersonnelFromCsvAsync(
                request.CsvContent,
                request.UpdateExisting);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Import işlemi sırasında hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Örnek CSV şablonu indirir
    /// </summary>
    [HttpGet("personnel/template")]
    [AllowAnonymous]
    public IActionResult GetTemplate()
    {
        var template = @"FullName,Username,Email,Password,Role,RoleId,Company
Ahmet Yılmaz,ahmet.yilmaz,a@b.com,user@123,CustomerOperator,3,Boyner
Mehmet Kaya,mehmet.kaya,mehmet.kaya@firma.com,user@123,CustomerManager,1,Boyner";

        var bytes = System.Text.Encoding.UTF8.GetBytes(template);
        return File(bytes, "text/csv", "personnel_import_template.csv");
    }

    #region Checklist Import

    /// <summary>
    /// CSV dosyasından yeni checklist oluşturur ve soruları import eder
    /// </summary>
    /// <param name="file">CSV dosyası</param>
    /// <param name="checklistName">Yeni checklist adı</param>
    /// <param name="customerId">Müşteri ID (opsiyonel)</param>
    /// <param name="description">Açıklama (opsiyonel)</param>
    /// <returns>Import sonucu</returns>
    [HttpPost("checklist")]
    [RequestSizeLimit(10 * 1024 * 1024)] // 10 MB limit
    public async Task<ActionResult<ChecklistImportResultDto>> ImportChecklist(
        IFormFile file,
        [FromQuery] string checklistName,
        [FromQuery] int? customerId = null,
        [FromQuery] string? description = null)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "Dosya seçilmedi." });
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (extension != ".csv")
            {
                return BadRequest(new { message = "Sadece CSV dosyaları kabul edilir." });
            }

            if (string.IsNullOrWhiteSpace(checklistName))
            {
                return BadRequest(new { message = "Checklist adı belirtmelisiniz." });
            }

            using var stream = file.OpenReadStream();
            var result = await _importService.ImportChecklistFromCsvAsync(stream, checklistName, customerId, description);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, CreateErrorResponse("Checklist import işlemi sırasında hata oluştu.", ex));
        }
    }

    /// <summary>
    /// Checklist import örnek CSV şablonu indirir
    /// </summary>
    [HttpGet("checklist/template")]
    [AllowAnonymous]
    public IActionResult GetChecklistTemplate()
    {
        var template = @"GroupName,QuestionText,WeightPoints,MaxPoints,ScoringType,PenaltyType,SubCriteria,Order,IsRequired,HelpText
İletişim,Müşterinin sözü kesildi / aynı anda konuşuldu,3,3,Scored,None,""Aynı anda konuşuldu.|Müşterinin sözü kesildi."",1,false,
Prosedür,Doğru prosedür uygulanmadı,15,1,Scored,None,""Adres teyidi eksik.|Teknik adımlar uygulanmadı."",2,false,
Kritik Hata,Kabul Edilemez Eylemler,100,1,Penalty,RedCard,""KVKK ihlali.|Müşteri ile polemiğe girme.|Sebepsiz görüşme sonlandırma."",3,true,Kırmızı kart gerektiren durumlar";

        var bytes = System.Text.Encoding.UTF8.GetBytes(template);
        return File(bytes, "text/csv", "checklist_import_template.csv");
    }

    #endregion
}

public class ImportRawRequest
{
    public string CsvContent { get; set; } = string.Empty;
    public bool UpdateExisting { get; set; } = false;
}
