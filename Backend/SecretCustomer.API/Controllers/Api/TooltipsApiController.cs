using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers.Api;

/// <summary>
/// Tooltip içeriklerini dönen API controller.
/// Partial view'ları HTML olarak döner.
/// </summary>
[ApiController]
[Route("api/tooltips")]
[Authorize]
public class TooltipsApiController : Controller
{
    /// <summary>
    /// Belirtilen key'e göre tooltip içeriğini döner
    /// </summary>
    /// <param name="key">Tooltip anahtarı (örn: project-types, user-roles)</param>
    /// <param name="id">Opsiyonel ID parametresi (dinamik içerik için)</param>
    [HttpGet("{key}")]
    public IActionResult GetTooltip(string key, [FromQuery] int? id)
    {
        return key.ToLower() switch
        {
            "project-types" => PartialView("~/Views/Shared/Tooltips/_ProjectTypes.cshtml"),
            "user-roles" => PartialView("~/Views/Shared/Tooltips/_UserRoles.cshtml"),
            "assignment-types" => PartialView("~/Views/Shared/Tooltips/_AssignmentTypes.cshtml"),
            _ => NotFound(new { message = $"Tooltip '{key}' bulunamadı." })
        };
    }
}
