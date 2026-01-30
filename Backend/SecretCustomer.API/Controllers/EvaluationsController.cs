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
    /// <param name="isInternal">true ise CustomerPersonnel (iç değerlendirme), false ise User (dış değerlendirme)</param>
    public IActionResult PopupMaximum(int? assignmentId, int? evaluationId, bool isInternal = false)
    {
        ViewBag.AssignmentId = assignmentId;
        ViewBag.EvaluationId = evaluationId;
        ViewBag.IsInternal = isInternal;
        return View();
    }

    /// <summary>
    /// CriteriaTotal modu için değerlendirme popup'ı (seçenek bazlı)
    /// </summary>
    /// <param name="isInternal">true ise CustomerPersonnel (iç değerlendirme), false ise User (dış değerlendirme)</param>
    public IActionResult PopupCriteriaTotal(int? assignmentId, int? evaluationId, bool isInternal = false)
    {
        ViewBag.AssignmentId = assignmentId;
        ViewBag.EvaluationId = evaluationId;
        ViewBag.IsInternal = isInternal;
        return View();
    }
}
