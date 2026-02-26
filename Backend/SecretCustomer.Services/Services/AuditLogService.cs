using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Security.Claims;
using SecretCustomer.Core.Helpers;

namespace SecretCustomer.Services.Services;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _httpContextAccessor = httpContextAccessor;
    }

    private (int? userId, string? userName, string? ipAddress, string? userAgent, string? requestUrl, string? httpMethod) GetRequestInfo()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
            return (null, null, null, null, null, null);

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userId = int.TryParse(userIdClaim, out var uid) ? uid : (int?)null;
        var userName = httpContext.User.FindFirst(ClaimTypes.Name)?.Value;

        var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = httpContext.Request.Headers["User-Agent"].FirstOrDefault();
        var requestUrl = $"{httpContext.Request.Path}{httpContext.Request.QueryString}";
        var httpMethod = httpContext.Request.Method;

        return (userId, userName, ipAddress, userAgent, requestUrl, httpMethod);
    }

    public async Task LogAsync(int logTypeId, string message, string? category = null, string? details = null)
    {
        var (userId, userName, ipAddress, userAgent, requestUrl, httpMethod) = GetRequestInfo();

        var log = new AuditLog
        {
            LogTypeId = logTypeId,
            Category = category,
            Message = message,
            Details = details,
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestUrl = requestUrl,
            HttpMethod = httpMethod,
            CreatedAt = TurkeyTime.Now
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogInfoAsync(string message, string? category = null, string? details = null)
    {
        await LogAsync(LogTypes.Ids.Info, message, category, details);
    }

    public async Task LogWarningAsync(string message, string? category = null, string? details = null)
    {
        await LogAsync(LogTypes.Ids.Warning, message, category, details);
    }

    public async Task LogErrorAsync(string message, string? category = null, Exception? exception = null)
    {
        var (userId, userName, ipAddress, userAgent, requestUrl, httpMethod) = GetRequestInfo();

        var log = new AuditLog
        {
            LogTypeId = LogTypes.Ids.Error,
            Category = category,
            Message = message,
            Details = exception?.Message,
            ExceptionType = exception?.GetType().FullName,
            StackTrace = exception?.StackTrace,
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestUrl = requestUrl,
            HttpMethod = httpMethod,
            CreatedAt = TurkeyTime.Now
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogDataChangeAsync(int logTypeId, string tableName, int recordId,
        string? oldValues = null, string? newValues = null, string? message = null)
    {
        var (userId, userName, ipAddress, userAgent, requestUrl, httpMethod) = GetRequestInfo();

        var actionName = logTypeId switch
        {
            LogTypes.Ids.DataCreate => "oluşturuldu",
            LogTypes.Ids.DataUpdate => "güncellendi",
            LogTypes.Ids.DataDelete => "silindi",
            _ => "değiştirildi"
        };

        var log = new AuditLog
        {
            LogTypeId = logTypeId,
            Category = "DataChange",
            Message = message ?? $"{tableName} kaydı {actionName}",
            TableName = tableName,
            RecordId = recordId,
            OldValues = oldValues,
            NewValues = newValues,
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestUrl = requestUrl,
            HttpMethod = httpMethod,
            CreatedAt = TurkeyTime.Now
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogLoginAsync(int userId, string userName, bool success, string? failReason = null)
    {
        var (_, _, ipAddress, userAgent, requestUrl, httpMethod) = GetRequestInfo();

        var log = new AuditLog
        {
            LogTypeId = success ? LogTypes.Ids.Login : LogTypes.Ids.LoginFailed,
            Category = "Auth",
            Message = success ? $"{userName} giriş yaptı" : $"{userName} giriş başarısız",
            Details = failReason,
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestUrl = requestUrl,
            HttpMethod = httpMethod,
            CreatedAt = TurkeyTime.Now
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogLogoutAsync(int userId, string userName)
    {
        var (_, _, ipAddress, userAgent, requestUrl, httpMethod) = GetRequestInfo();

        var log = new AuditLog
        {
            LogTypeId = LogTypes.Ids.Logout,
            Category = "Auth",
            Message = $"{userName} çıkış yaptı",
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestUrl = requestUrl,
            HttpMethod = httpMethod,
            CreatedAt = TurkeyTime.Now
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogAccessDeniedAsync(string resource, string? reason = null)
    {
        var (userId, userName, ipAddress, userAgent, requestUrl, httpMethod) = GetRequestInfo();

        var log = new AuditLog
        {
            LogTypeId = LogTypes.Ids.AccessDenied,
            Category = "Auth",
            Message = $"Erişim reddedildi: {resource}",
            Details = reason,
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestUrl = requestUrl,
            HttpMethod = httpMethod,
            CreatedAt = TurkeyTime.Now
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(
        int? logTypeId = null,
        string? category = null,
        int? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50,
        int? customerId = null)
    {
        var query = BuildLogsQuery(logTypeId, category, userId, fromDate, toDate, customerId);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetLogsCountAsync(
        int? logTypeId = null,
        string? category = null,
        int? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int? customerId = null)
    {
        var query = BuildLogsQuery(logTypeId, category, userId, fromDate, toDate, customerId);
        return await query.CountAsync();
    }

    private IQueryable<AuditLog> BuildLogsQuery(
        int? logTypeId, string? category, int? userId,
        DateTime? fromDate, DateTime? toDate, int? customerId)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (logTypeId.HasValue)
            query = query.Where(l => l.LogTypeId == logTypeId.Value);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(l => l.Category == category);

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);

        if (customerId.HasValue)
        {
            // Müşterinin projelerinde atanmış kullanıcıların logları
            var customerUserIds = _context.Assignments
                .Where(a => a.Project != null && a.Project.CustomerId == customerId.Value && a.AssignedUserId.HasValue)
                .Select(a => a.AssignedUserId!.Value)
                .Distinct();

            query = query.Where(l => l.UserId.HasValue && customerUserIds.Contains(l.UserId.Value));
        }

        if (fromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.CreatedAt <= toDate.Value);

        return query;
    }

    public async Task<AuditLog?> GetByIdAsync(int id)
    {
        return await _context.AuditLogs.FindAsync(id);
    }

    public async Task<int> DeleteOldLogsAsync(int daysToKeep)
    {
        var cutoffDate = TurkeyTime.Now.AddDays(-daysToKeep);
        var oldLogs = await _context.AuditLogs
            .Where(l => l.CreatedAt < cutoffDate)
            .ToListAsync();

        _context.AuditLogs.RemoveRange(oldLogs);
        return await _context.SaveChangesAsync();
    }
}
