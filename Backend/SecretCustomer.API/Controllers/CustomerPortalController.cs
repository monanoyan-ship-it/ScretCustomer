using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Müşteri Portalı - MVC Controller
/// Müşteri personelinin giriş yapıp değerlendirme sonuçlarını görüntüleyeceği portal
/// </summary>
[Authorize(Roles = "CustomerManager,CustomerSupervisor,CustomerOperator")]
public class CustomerPortalController : Controller
{
    /// <summary>
    /// Müşteri portalı dashboard
    /// </summary>
    public IActionResult Dashboard()
    {
        return View();
    }

    /// <summary>
    /// Değerlendirme sonuçları
    /// </summary>
    public IActionResult Evaluations()
    {
        return View();
    }

    /// <summary>
    /// Raporlar
    /// </summary>
    public IActionResult Reports()
    {
        return View();
    }

    /// <summary>
    /// Şube bazlı sonuçlar (sadece CustomerManager)
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager")]
    public IActionResult Branches()
    {
        return View();
    }

    /// <summary>
    /// Kullanıcı profili ve şifre değiştirme
    /// </summary>
    public IActionResult Profile()
    {
        return View();
    }
}
