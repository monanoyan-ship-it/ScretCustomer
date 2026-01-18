using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Kullanıcının kendi eğitimlerini görüntüleme sayfası
/// </summary>
[Authorize]
public class MyTrainingsController : Controller
{
    /// <summary>
    /// Kullanıcıya atanan eğitimler sayfası
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }
}
