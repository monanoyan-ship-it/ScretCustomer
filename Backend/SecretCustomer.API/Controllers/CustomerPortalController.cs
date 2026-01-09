using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Müşteri Portalı - MVC Controller
/// Müşteri personelinin giriş yapıp değerlendirme sonuçlarını görüntüleyeceği portal
/// </summary>
public class CustomerPortalController : Controller
{
    /// <summary>
    /// Müşteri portalı login - Ana login sayfasına yönlendir
    /// </summary>
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // CustomerPersonnel artık ana login sayfasını kullanıyor
        return RedirectToAction("Login", "Account");
    }

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
    /// Şube bazlı sonuçlar
    /// </summary>
    public IActionResult Branches()
    {
        return View();
    }

    /// <summary>
    /// İç Değerlendirme Atamaları - Sadece Manager ve Supervisor
    /// </summary>
    public IActionResult InternalAssignments()
    {
        return View();
    }

    /// <summary>
    /// Çıkış yap
    /// </summary>
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login", "Account");
    }
}
