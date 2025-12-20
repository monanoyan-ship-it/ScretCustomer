using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers.Api;

[Route("api/auditlogs")]
[ApiController]
[Authorize(Roles = "Admin")]
public class AuditLogsApiController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsApiController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] LogType? logType = null,
        [FromQuery] string? category = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var logs = await _auditLogService.GetLogsAsync(
            logType, category, userId, fromDate, toDate, page, pageSize);

        var totalCount = await _auditLogService.GetLogsCountAsync(
            logType, category, userId, fromDate, toDate);

        return Ok(new
        {
            data = logs.Select(l => new
            {
                l.Id,
                logType = (int)l.LogType,
                logTypeName = GetLogTypeName(l.LogType),
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
            }),
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
            logType = (int)log.LogType,
            logTypeName = GetLogTypeName(log.LogType),
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
    public IActionResult GetLogTypes()
    {
        var types = Enum.GetValues<LogType>()
            .Select(t => new { value = (int)t, name = GetLogTypeName(t) });

        return Ok(types);
    }

    [HttpDelete("cleanup")]
    public async Task<IActionResult> CleanupOldLogs([FromQuery] int daysToKeep = 90)
    {
        var deletedCount = await _auditLogService.DeleteOldLogsAsync(daysToKeep);
        return Ok(new { deletedCount, message = $"{deletedCount} eski log silindi" });
    }

    private static string GetLogTypeName(LogType logType)
    {
        return logType switch
        {
            LogType.Info => "Bilgi",
            LogType.Warning => "Uyarı",
            LogType.Error => "Hata",
            LogType.DataCreate => "Veri Oluşturma",
            LogType.DataUpdate => "Veri Güncelleme",
            LogType.DataDelete => "Veri Silme",
            LogType.Login => "Giriş",
            LogType.Logout => "Çıkış",
            LogType.LoginFailed => "Başarısız Giriş",
            LogType.AccessDenied => "Erişim Reddedildi",
            _ => logType.ToString()
        };
    }
}
