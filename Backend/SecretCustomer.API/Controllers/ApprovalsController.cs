using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Onay yönetimi MVC controller
/// </summary>
[Authorize(Roles = "Admin,QualitySpecialist")]
public class ApprovalsController : Controller
{
    /// <summary>
    /// Onay yönetimi sayfası (Liste + Detail modali)
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }
}
