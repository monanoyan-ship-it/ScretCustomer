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
    /// Maksimum Puanlama Editörü
    /// </summary>
    /// <param name="id">Düzenlenecek checklist ID</param>
    /// <param name="clone">Klonlanacak checklist ID</param>
    public IActionResult Maximum(int? id, int? clone)
    {
        ViewBag.ChecklistId = id;
        ViewBag.CloneId = clone;
        return View("EditorMaximum");
    }

    /// <summary>
    /// Kriter Toplam Puanlama Editörü
    /// </summary>
    /// <param name="id">Düzenlenecek checklist ID</param>
    /// <param name="clone">Klonlanacak checklist ID</param>
    public IActionResult CriteriaTotal(int? id, int? clone)
    {
        ViewBag.ChecklistId = id;
        ViewBag.CloneId = clone;
        return View("EditorCriteriaTotal");
    }

    /// <summary>
    /// Anket Editörü
    /// </summary>
    /// <param name="id">Düzenlenecek checklist ID</param>
    /// <param name="clone">Klonlanacak checklist ID</param>
    public IActionResult Survey(int? id, int? clone)
    {
        ViewBag.ChecklistId = id;
        ViewBag.CloneId = clone;
        return View("EditorSurvey");
    }

    /// <summary>
    /// Personel Değerlendirme Editörü (EditorSurvey + SelfText/SelfDescription)
    /// </summary>
    public IActionResult Assessment(int? id, int? clone)
    {
        ViewBag.ChecklistId = id;
        ViewBag.CloneId = clone;
        ViewBag.IsAssessment = true;
        return View("EditorSurvey");
    }
}
