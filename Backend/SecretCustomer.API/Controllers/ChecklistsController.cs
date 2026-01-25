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
    /// <param name="id">Düzenlenecek checklist ID</param>
    /// <param name="clone">Klonlanacak checklist ID</param>
    /// <param name="scoringMethod">Puanlama yöntemi (Maximum veya CriteriaTotal)</param>
    public IActionResult Editor(int? id, int? clone, string? scoringMethod)
    {
        ViewBag.ChecklistId = id;
        ViewBag.CloneId = clone;
        ViewBag.ScoringMethod = scoringMethod ?? "Maximum";
        return View();
    }
}
