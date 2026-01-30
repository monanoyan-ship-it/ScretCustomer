using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Eğitim videosu anket (quiz) yönetimi MVC controller
/// </summary>
[Authorize(Roles = "Admin")]
public class TrainingQuizController : Controller
{
    /// <summary>
    /// Anket yönetimi sayfası (Liste + CRUD modalleri)
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }
}
