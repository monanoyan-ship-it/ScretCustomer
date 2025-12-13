using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Bildirim yönetimi MVC controller
/// </summary>
[Authorize]
public class NotificationsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Settings()
    {
        return View();
    }
}
