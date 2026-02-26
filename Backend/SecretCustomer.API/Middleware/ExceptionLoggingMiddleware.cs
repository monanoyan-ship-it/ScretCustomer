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

    public ExceptionLoggingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLogService auditLogService)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
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
        catch
        {
            // Loglama başarısız olursa sessizce devam et
        }

        // Response hazırla
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        // ShowDetailedErrors ayarını kontrol et
        var config = context.RequestServices.GetService<IConfiguration>();
        var showDetails = config?.GetValue<bool>("ShowDetailedErrors") ?? false;

        object response;
        if (showDetails)
        {
            response = new
            {
                error = "Bir hata oluştu.",
                message = exception.Message,
                stackTrace = exception.StackTrace,
                innerException = exception.InnerException?.Message,
                exceptionType = exception.GetType().Name
            };
        }
        else
        {
            response = new
            {
                error = "Bir hata oluştu. Lütfen daha sonra tekrar deneyin."
            };
        }

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
