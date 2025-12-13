using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin,TeamLeader")]
public class PersonnelController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
