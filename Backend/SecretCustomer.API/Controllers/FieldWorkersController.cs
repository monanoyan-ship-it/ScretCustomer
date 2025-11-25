using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin")]
public class FieldWorkersController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
