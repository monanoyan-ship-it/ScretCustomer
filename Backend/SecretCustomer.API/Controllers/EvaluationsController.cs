using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize]
public class EvaluationsController : Controller
{
    // TEK ACTION - Index (SPA Modal Pattern)
    // Detay ve Değerlendirme işlemleri modal ile yapılıyor
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Değerlendirme Formu - Checklist doldurma sayfası
    /// </summary>
    /// <param name="id">Assignment ID</param>
    public IActionResult Form(int id)
    {
        ViewBag.AssignmentId = id;
        return View();
    }
}
