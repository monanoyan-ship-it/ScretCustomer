using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin")]
public class OrganizationController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Delegations()
    {
        return View();
    }
}
