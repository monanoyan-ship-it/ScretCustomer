using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using System.Net;
using System.Text.Json;

namespace SecretCustomer.API.Middleware;

/// <summary>
/// Tüm unhandled exception'ları yakalar ve AuditLog'a yazar
/// </summary>
public class ExceptionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionLoggingMiddleware> _logger;

    public ExceptionLoggingMiddleware(RequestDelegate next, ILogger<ExceptionLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex, auditLogService);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception, IAuditLogService auditLogService)
    {
        // Log to database
        try
        {
            await auditLogService.LogErrorAsync(
                exception.Message,
                "UnhandledException",
                exception
            );
        }
        catch (Exception logEx)
        {
            // Loglama başarısız olursa sadece console'a yaz
            _logger.LogError(logEx, "Failed to log exception to database");
        }

        // Response hazırla
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var response = new
        {
            error = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin.",
            details = context.RequestServices.GetService<IWebHostEnvironment>()?.IsDevelopment() == true
                ? exception.Message
                : null
        };

        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
    }
}

/// <summary>
/// Middleware extension
/// </summary>
public static class ExceptionLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionLogging(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionLoggingMiddleware>();
    }
}
