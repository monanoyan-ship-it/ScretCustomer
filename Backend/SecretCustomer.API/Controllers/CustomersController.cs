using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin")]
public class CustomersController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Dealers(int id)
    {
        ViewBag.CustomerId = id;
        return View();
    }
}
