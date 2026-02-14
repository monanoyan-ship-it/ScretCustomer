using ClosedXML.Excel;
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
    /// Örnek Excel şablonu indirir
    /// </summary>
    [HttpGet("personnel/template")]
    [AllowAnonymous]
    public IActionResult GetTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Personnel");

        // Header
        var headers = new[] { "FullName", "Username", "Email", "Password", "Role", "RoleId", "Company" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        // Header style
        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Sample data
        var row2 = new[] { "Ahmet Yılmaz", "ahmet.yilmaz", "a@b.com", "user@123", "CustomerOperator", "3", "Boyner" };
        var row3 = new[] { "Mehmet Kaya", "mehmet.kaya", "mehmet.kaya@firma.com", "user@123", "CustomerManager", "1", "Boyner" };
        for (int i = 0; i < row2.Length; i++)
        {
            ws.Cell(2, i + 1).Value = row2[i];
            ws.Cell(3, i + 1).Value = row3[i];
        }

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "personnel_import_template.xlsx");
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
    /// Checklist import örnek Excel şablonu indirir
    /// </summary>
    [HttpGet("checklist/template")]
    [AllowAnonymous]
    public IActionResult GetChecklistTemplate()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Checklist");

        // Header
        var headers = new[] { "GroupName", "QuestionText", "WeightPoints", "MaxPoints", "ScoringType", "PenaltyType", "SubCriteria", "Order", "IsRequired", "HelpText" };
        for (int i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        // Header style
        var headerRange = ws.Range(1, 1, 1, headers.Length);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Sample data row 1
        ws.Cell(2, 1).Value = "İletişim";
        ws.Cell(2, 2).Value = "Müşterinin sözü kesildi / aynı anda konuşuldu";
        ws.Cell(2, 3).Value = 3;
        ws.Cell(2, 4).Value = 3;
        ws.Cell(2, 5).Value = "Scored";
        ws.Cell(2, 6).Value = "None";
        ws.Cell(2, 7).Value = "Aynı anda konuşuldu.|Müşterinin sözü kesildi.";
        ws.Cell(2, 8).Value = 1;
        ws.Cell(2, 9).Value = "false";
        ws.Cell(2, 10).Value = "";

        // Sample data row 2
        ws.Cell(3, 1).Value = "Prosedür";
        ws.Cell(3, 2).Value = "Doğru prosedür uygulanmadı";
        ws.Cell(3, 3).Value = 15;
        ws.Cell(3, 4).Value = 1;
        ws.Cell(3, 5).Value = "Scored";
        ws.Cell(3, 6).Value = "None";
        ws.Cell(3, 7).Value = "Adres teyidi eksik.|Teknik adımlar uygulanmadı.";
        ws.Cell(3, 8).Value = 2;
        ws.Cell(3, 9).Value = "false";
        ws.Cell(3, 10).Value = "";

        // Sample data row 3
        ws.Cell(4, 1).Value = "Kritik Hata";
        ws.Cell(4, 2).Value = "Kabul Edilemez Eylemler";
        ws.Cell(4, 3).Value = 100;
        ws.Cell(4, 4).Value = 1;
        ws.Cell(4, 5).Value = "Penalty";
        ws.Cell(4, 6).Value = "RedCard";
        ws.Cell(4, 7).Value = "KVKK ihlali.|Müşteri ile polemiğe girme.|Sebepsiz görüşme sonlandırma.";
        ws.Cell(4, 8).Value = 3;
        ws.Cell(4, 9).Value = "true";
        ws.Cell(4, 10).Value = "Kırmızı kart gerektiren durumlar";

        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "checklist_import_template.xlsx");
    }

    #endregion
}

public class ImportRawRequest
{
    public string CsvContent { get; set; } = string.Empty;
    public bool UpdateExisting { get; set; } = false;
}
