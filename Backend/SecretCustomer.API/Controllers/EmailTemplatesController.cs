using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin")]
public class EmailTemplatesController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    // Popup editor for create/edit
    public IActionResult Editor(int? id = null)
    {
        ViewBag.TemplateId = id;
        return View();
    }
}
