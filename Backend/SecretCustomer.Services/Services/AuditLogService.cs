using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using System.Security.Claims;

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

    public async Task LogAsync(LogType logType, string message, string? category = null, string? details = null)
    {
        var (userId, userName, ipAddress, userAgent, requestUrl, httpMethod) = GetRequestInfo();

        var log = new AuditLog
        {
            LogType = logType,
            Category = category,
            Message = message,
            Details = details,
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestUrl = requestUrl,
            HttpMethod = httpMethod,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogInfoAsync(string message, string? category = null, string? details = null)
    {
        await LogAsync(LogType.Info, message, category, details);
    }

    public async Task LogWarningAsync(string message, string? category = null, string? details = null)
    {
        await LogAsync(LogType.Warning, message, category, details);
    }

    public async Task LogErrorAsync(string message, string? category = null, Exception? exception = null)
    {
        var (userId, userName, ipAddress, userAgent, requestUrl, httpMethod) = GetRequestInfo();

        var log = new AuditLog
        {
            LogType = LogType.Error,
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
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogDataChangeAsync(LogType logType, string tableName, int recordId,
        string? oldValues = null, string? newValues = null, string? message = null)
    {
        var (userId, userName, ipAddress, userAgent, requestUrl, httpMethod) = GetRequestInfo();

        var actionName = logType switch
        {
            LogType.DataCreate => "oluşturuldu",
            LogType.DataUpdate => "güncellendi",
            LogType.DataDelete => "silindi",
            _ => "değiştirildi"
        };

        var log = new AuditLog
        {
            LogType = logType,
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
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogLoginAsync(int userId, string userName, bool success, string? failReason = null)
    {
        var (_, _, ipAddress, userAgent, requestUrl, httpMethod) = GetRequestInfo();

        var log = new AuditLog
        {
            LogType = success ? LogType.Login : LogType.LoginFailed,
            Category = "Auth",
            Message = success ? $"{userName} giriş yaptı" : $"{userName} giriş başarısız",
            Details = failReason,
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestUrl = requestUrl,
            HttpMethod = httpMethod,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogLogoutAsync(int userId, string userName)
    {
        var (_, _, ipAddress, userAgent, requestUrl, httpMethod) = GetRequestInfo();

        var log = new AuditLog
        {
            LogType = LogType.Logout,
            Category = "Auth",
            Message = $"{userName} çıkış yaptı",
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestUrl = requestUrl,
            HttpMethod = httpMethod,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task LogAccessDeniedAsync(string resource, string? reason = null)
    {
        var (userId, userName, ipAddress, userAgent, requestUrl, httpMethod) = GetRequestInfo();

        var log = new AuditLog
        {
            LogType = LogType.AccessDenied,
            Category = "Auth",
            Message = $"Erişim reddedildi: {resource}",
            Details = reason,
            UserId = userId,
            UserName = userName,
            IpAddress = ipAddress,
            UserAgent = userAgent,
            RequestUrl = requestUrl,
            HttpMethod = httpMethod,
            CreatedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(
        LogType? logType = null,
        string? category = null,
        int? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (logType.HasValue)
            query = query.Where(l => l.LogType == logType.Value);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(l => l.Category == category);

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);

        if (fromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetLogsCountAsync(
        LogType? logType = null,
        string? category = null,
        int? userId = null,
        DateTime? fromDate = null,
        DateTime? toDate = null)
    {
        var query = _context.AuditLogs.AsQueryable();

        if (logType.HasValue)
            query = query.Where(l => l.LogType == logType.Value);

        if (!string.IsNullOrEmpty(category))
            query = query.Where(l => l.Category == category);

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);

        if (fromDate.HasValue)
            query = query.Where(l => l.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(l => l.CreatedAt <= toDate.Value);

        return await query.CountAsync();
    }

    public async Task<AuditLog?> GetByIdAsync(int id)
    {
        return await _context.AuditLogs.FindAsync(id);
    }

    public async Task<int> DeleteOldLogsAsync(int daysToKeep)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);
        var oldLogs = await _context.AuditLogs
            .Where(l => l.CreatedAt < cutoffDate)
            .ToListAsync();

        _context.AuditLogs.RemoveRange(oldLogs);
        return await _context.SaveChangesAsync();
    }
}
