using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Public survey form controller - No authentication required
/// </summary>
public class SurveyController : Controller
{
    /// <summary>
    /// Survey form page - Token is validated client-side via API
    /// </summary>
    [AllowAnonymous]
    public IActionResult Form(string? token = null)
    {
        ViewBag.Token = token;
        return View();
    }

    /// <summary>
    /// Survey completed page
    /// </summary>
    [AllowAnonymous]
    public IActionResult Completed()
    {
        return View();
    }
}
