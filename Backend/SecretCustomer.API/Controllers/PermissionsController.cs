using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin")]
public class PermissionsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult RolePermissions()
    {
        return View();
    }

    public IActionResult UserPermissions()
    {
        return View();
    }
}
