using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
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
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var logs = await _auditLogService.GetLogsAsync(
            logTypeId, category, userId, fromDate, toDate, page, pageSize);

        var totalCount = await _auditLogService.GetLogsCountAsync(
            logTypeId, category, userId, fromDate, toDate);

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

    [HttpDelete("cleanup")]
    public async Task<IActionResult> CleanupOldLogs([FromQuery] int daysToKeep = 90)
    {
        var deletedCount = await _auditLogService.DeleteOldLogsAsync(daysToKeep);
        return Ok(new { deletedCount, message = $"{deletedCount} eski log silindi" });
    }
}
