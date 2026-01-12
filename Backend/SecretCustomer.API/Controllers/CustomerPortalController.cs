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
    /// Proje bazlı sonuçlar (sadece CustomerManager)
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager")]
    public IActionResult Branches()
    {
        return View();
    }

    /// <summary>
    /// Organizasyonlar/Şubeler (sadece CustomerManager)
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager")]
    public IActionResult Organizations()
    {
        return View();
    }

    /// <summary>
    /// Süpervizörler (sadece CustomerManager)
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager")]
    public IActionResult Supervisors()
    {
        return View();
    }

    /// <summary>
    /// İç dinlemeler - firma personeli tarafından yapılan (sadece CustomerManager)
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager")]
    public IActionResult InternalEvaluations()
    {
        return View();
    }

    /// <summary>
    /// Dış dinlemeler - bizim tarafımızdan yapılan (sadece CustomerManager)
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager")]
    public IActionResult ExternalEvaluations()
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
