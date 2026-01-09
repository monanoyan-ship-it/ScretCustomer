using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Helpers;

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
            var innermost = ExceptionHelper.GetInnermostException(ex);
            return new
            {
                message = message,
                error = ex.Message,
                innermostException = innermost.Message,
                stackTrace = ex.StackTrace,
                exceptionType = ex.GetType().Name
            };
        }
        return new { message = message };
    }
}
