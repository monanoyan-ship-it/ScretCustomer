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
    /// Degerlendirme formu sayfasi
    /// </summary>
    public IActionResult Evaluate(Guid? assignmentId, Guid? evaluationId)
    {
        if (assignmentId.HasValue)
        {
            ViewBag.AssignmentId = assignmentId.Value;
            ViewBag.EvaluationId = null;
        }
        else if (evaluationId.HasValue)
        {
            ViewBag.AssignmentId = null;
            ViewBag.EvaluationId = evaluationId.Value;
        }
        else
        {
            return RedirectToAction("Index");
        }

        return View();
    }

    /// <summary>
    /// Degerlendirme detay sayfasi (salt okunur)
    /// </summary>
    public IActionResult Details(Guid id)
    {
        ViewBag.EvaluationId = id;
        return View();
    }
}
