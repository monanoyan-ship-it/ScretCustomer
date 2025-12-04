using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin")]
public class ExcelTemplatesController : Controller
{
    // GET: /ExcelTemplates/Index
    public IActionResult Index()
    {
        return View();
    }
}
