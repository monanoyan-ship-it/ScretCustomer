using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Banka Gizli Müşteri Ziyaretleri MVC Controller (GBF - Gizli Banka Formu)
/// </summary>
[Authorize]
public class BankVisitsController : Controller
{
    /// <summary>
    /// Banka ziyaretleri listesi sayfası (Create/Edit/Detail modal ile)
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }
}
