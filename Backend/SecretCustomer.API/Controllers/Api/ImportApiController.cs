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
}

public class ImportRawRequest
{
    public string CsvContent { get; set; } = string.Empty;
    public bool UpdateExisting { get; set; } = false;
}
