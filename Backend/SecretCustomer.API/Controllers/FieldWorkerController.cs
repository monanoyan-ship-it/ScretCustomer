using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "FieldWorker,Admin,QualitySpecialist")]
public class FieldWorkerController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Visits()
    {
        return View();
    }

    public IActionResult Requests()
    {
        return View();
    }
}
