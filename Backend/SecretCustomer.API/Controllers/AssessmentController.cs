using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Public assessment form controller - No authentication required
/// </summary>
public class AssessmentController : Controller
{
    /// <summary>
    /// Assessment fill page - Token is validated client-side via API
    /// </summary>
    [AllowAnonymous]
    public IActionResult Fill(string? token = null)
    {
        ViewBag.Token = token;
        return View();
    }

    /// <summary>
    /// Assessment completed page
    /// </summary>
    [AllowAnonymous]
    public IActionResult Completed()
    {
        return View();
    }
}
