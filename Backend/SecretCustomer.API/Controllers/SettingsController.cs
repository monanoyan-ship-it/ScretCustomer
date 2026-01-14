using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin")]
public class SettingsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Smtp()
    {
        return View();
    }
}
