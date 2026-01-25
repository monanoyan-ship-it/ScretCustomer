using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize]
public class EvaluationsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Maximum modu için değerlendirme popup'ı (klasik 0-5 puan girişi)
    /// </summary>
    public IActionResult PopupMaximum(int? assignmentId, int? evaluationId)
    {
        ViewBag.AssignmentId = assignmentId;
        ViewBag.EvaluationId = evaluationId;
        return View();
    }

    /// <summary>
    /// CriteriaTotal modu için değerlendirme popup'ı (seçenek bazlı)
    /// </summary>
    public IActionResult PopupCriteriaTotal(int? assignmentId, int? evaluationId)
    {
        ViewBag.AssignmentId = assignmentId;
        ViewBag.EvaluationId = evaluationId;
        return View();
    }
}
