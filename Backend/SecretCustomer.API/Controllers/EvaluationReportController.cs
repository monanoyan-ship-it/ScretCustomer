using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Public değerlendirme raporu sayfası.
/// Email'deki linklerden erişilir, authentication gerektirmez.
/// </summary>
[AllowAnonymous]
public class EvaluationReportController : Controller
{
    [Route("report/view/{token}")]
    public IActionResult ViewReport(string token)
    {
        ViewData["Title"] = "Değerlendirme Raporu";
        ViewData["Token"] = token;
        return View("Index");
    }
}
