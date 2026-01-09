using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize]
public class InternalAssignmentsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
