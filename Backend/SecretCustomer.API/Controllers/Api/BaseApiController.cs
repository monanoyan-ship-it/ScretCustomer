using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers.Api;

/// <summary>
/// Tüm API Controller'lar için base class
/// ShowDetailedErrors ayarına göre hata detayı döndürür
/// </summary>
public abstract class BaseApiController : ControllerBase
{
    protected readonly IConfiguration Configuration;

    protected BaseApiController(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// ShowDetailedErrors=true ise exception detaylarını döndürür
    /// </summary>
    protected object CreateErrorResponse(string message, Exception? ex = null)
    {
        var showDetails = Configuration.GetValue<bool>("ShowDetailedErrors");
        if (showDetails && ex != null)
        {
            return new
            {
                message = message,
                error = ex.Message,
                innerException = ex.InnerException?.Message,
                stackTrace = ex.StackTrace,
                exceptionType = ex.GetType().Name
            };
        }
        return new { message = message };
    }
}
