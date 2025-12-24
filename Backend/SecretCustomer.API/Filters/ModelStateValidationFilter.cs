using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SecretCustomer.API.Filters;

/// <summary>
/// Tüm API Controller'larda ModelState hatalarını detaylı gösterir
/// </summary>
public class ModelStateValidationFilter : IActionFilter
{
    private readonly IConfiguration _configuration;

    public ModelStateValidationFilter(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Sadece API controller'lar için çalış
        if (!context.HttpContext.Request.Path.StartsWithSegments("/api"))
            return;

        if (!context.ModelState.IsValid)
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .ToDictionary(
                    k => k.Key,
                    v => v.Value!.Errors.Select(e => string.IsNullOrEmpty(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage).ToArray()
                );

            var showDetails = _configuration.GetValue<bool>("ShowDetailedErrors");
            var errorMessages = errors.SelectMany(e => e.Value).Where(m => !string.IsNullOrEmpty(m)).ToList();
            var combinedMessage = string.Join("; ", errorMessages);

            object response;
            if (showDetails)
            {
                response = new
                {
                    message = "Validasyon hatası",
                    error = combinedMessage,
                    validationErrors = errors,
                    fields = errors.Keys.ToArray()
                };
            }
            else
            {
                response = new { message = "Validasyon hatası: " + combinedMessage };
            }

            context.Result = new BadRequestObjectResult(response);
        }
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Nothing to do
    }
}
