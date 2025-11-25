using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin")]
public class ChecklistsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
