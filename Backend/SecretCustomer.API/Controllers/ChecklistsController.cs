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

    /// <summary>
    /// Checklist Editor - Popup olarak açılır
    /// </summary>
    public IActionResult Editor(int? id, int? clone)
    {
        ViewBag.ChecklistId = id;
        ViewBag.CloneId = clone;
        return View();
    }
}
