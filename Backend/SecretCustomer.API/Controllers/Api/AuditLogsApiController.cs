using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/auditlogs")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AuditLogsApiController : BaseApiController
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILocalizationService _localizationService;

    public AuditLogsApiController(
        IAuditLogService auditLogService,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _auditLogService = auditLogService;
        _localizationService = localizationService;
    }

    private async Task<string> GetLogTypeNameAsync(int logTypeId)
    {
        var item = LogTypes.GetById(logTypeId);
        if (item == null) return "Bilinmiyor";
        return await _localizationService.GetResourceAsync(item.NameResourceKey, (int?)null, item.Description);
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int? logTypeId = null,
        [FromQuery] string? category = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? userName = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] int? customerId = null)
    {
        var logs = await _auditLogService.GetLogsAsync(
            logTypeId, category, userId, fromDate, toDate, page, pageSize, customerId, userName);

        var totalCount = await _auditLogService.GetLogsCountAsync(
            logTypeId, category, userId, fromDate, toDate, customerId, userName);

        var data = new List<object>();
        foreach (var l in logs)
        {
            data.Add(new
            {
                l.Id,
                logTypeId = l.LogTypeId,
                logTypeName = await GetLogTypeNameAsync(l.LogTypeId),
                l.Category,
                l.Message,
                l.Details,
                l.TableName,
                l.RecordId,
                l.OldValues,
                l.NewValues,
                l.UserId,
                l.UserName,
                l.IpAddress,
                l.UserAgent,
                l.RequestUrl,
                l.HttpMethod,
                l.ExceptionType,
                l.StackTrace,
                l.CreatedAt
            });
        }

        return Ok(new
        {
            data,
            totalCount,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetLog(int id)
    {
        var log = await _auditLogService.GetByIdAsync(id);
        if (log == null)
            return NotFound();

        return Ok(new
        {
            log.Id,
            logTypeId = log.LogTypeId,
            logTypeName = await GetLogTypeNameAsync(log.LogTypeId),
            log.Category,
            log.Message,
            log.Details,
            log.TableName,
            log.RecordId,
            log.OldValues,
            log.NewValues,
            log.UserId,
            log.UserName,
            log.IpAddress,
            log.UserAgent,
            log.RequestUrl,
            log.HttpMethod,
            log.ExceptionType,
            log.StackTrace,
            log.CreatedAt
        });
    }

    [HttpGet("types")]
    public async Task<IActionResult> GetLogTypes()
    {
        var types = new List<object>();
        foreach (var t in LogTypes.All)
        {
            types.Add(new { value = t.Id, name = await GetLogTypeNameAsync(t.Id) });
        }

        return Ok(types);
    }

    [HttpGet("export")]
    public async Task<IActionResult> ExportToExcel(
        [FromQuery] int? logTypeId = null,
        [FromQuery] string? category = null,
        [FromQuery] int? userId = null,
        [FromQuery] string? userName = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int? customerId = null)
    {
        try
        {
            var logs = await _auditLogService.GetLogsForExportAsync(
                logTypeId, category, userId, fromDate, toDate, customerId, userName);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Sistem Logları");

            // Başlık satırı
            var headers = new[] { "Tarih", "Log Tipi", "Kategori", "Mesaj", "Kullanıcı", "IP Adresi", "URL", "HTTP Method" };
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
            }

            // Başlık stili
            var headerRow = worksheet.Row(1);
            headerRow.Style.Font.Bold = true;
            headerRow.Style.Fill.BackgroundColor = XLColor.LightGray;

            // Veri satırları
            for (int row = 0; row < logs.Count; row++)
            {
                var l = logs[row];
                var currentRow = row + 2;

                worksheet.Cell(currentRow, 1).Value = l.CreatedAt.ToString("dd.MM.yyyy HH:mm:ss");
                worksheet.Cell(currentRow, 2).Value = await GetLogTypeNameAsync(l.LogTypeId);
                worksheet.Cell(currentRow, 3).Value = l.Category ?? "-";
                worksheet.Cell(currentRow, 4).Value = l.Message ?? "-";
                worksheet.Cell(currentRow, 5).Value = l.UserName ?? "-";
                worksheet.Cell(currentRow, 6).Value = l.IpAddress ?? "-";
                worksheet.Cell(currentRow, 7).Value = l.RequestUrl ?? "-";
                worksheet.Cell(currentRow, 8).Value = l.HttpMethod ?? "-";
            }

            // Sütun genişliklerini ayarla
            worksheet.Columns().AdjustToContents();

            // Minimum genişlikler
            if (worksheet.Column(1).Width < 18) worksheet.Column(1).Width = 18;
            if (worksheet.Column(4).Width > 60) worksheet.Column(4).Width = 60;
            if (worksheet.Column(7).Width > 50) worksheet.Column(7).Width = 50;

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"SistemLoglari_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error exporting audit logs to Excel", "AuditLogs", ex);
            return StatusCode(500, new { error = "Excel dışa aktarma hatası" });
        }
    }

    [HttpDelete("cleanup")]
    public async Task<IActionResult> CleanupOldLogs([FromQuery] int daysToKeep = 90)
    {
        var deletedCount = await _auditLogService.DeleteOldLogsAsync(daysToKeep);
        return Ok(new { deletedCount, message = $"{deletedCount} eski log silindi" });
    }
}
