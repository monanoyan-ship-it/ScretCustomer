using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Eğitim videoları yönetimi MVC controller
/// </summary>
[Authorize(Roles = "Admin")]
public class TrainingVideosController : Controller
{
    /// <summary>
    /// Eğitim videoları yönetimi sayfası (Liste + Upload/Edit/Delete modalleri)
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }

    /// <summary>
    /// Video atama yönetimi sayfası
    /// </summary>
    public IActionResult Assignments()
    {
        return View();
    }

    /// <summary>
    /// Dış katılımcılar popup sayfası
    /// </summary>
    public IActionResult ExternalParticipants(int id)
    {
        ViewBag.AssignmentId = id;
        return View();
    }
}
