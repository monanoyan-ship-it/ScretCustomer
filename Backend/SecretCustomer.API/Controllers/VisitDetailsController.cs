using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Ziyaret Detayları Yönetim Sayfası - Sektör ve Alan Tanımları
/// </summary>
[Authorize(Roles = "Admin")]
public class VisitDetailsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
