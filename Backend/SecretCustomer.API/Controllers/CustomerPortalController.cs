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
    /// Müşteri portalı login sayfası
    /// </summary>
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        // Eğer zaten giriş yapmışsa dashboard'a yönlendir
        if (Request.Cookies.ContainsKey("CustomerToken"))
        {
            return RedirectToAction(nameof(Dashboard));
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
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
    /// Çıkış yap
    /// </summary>
    [AllowAnonymous]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("CustomerToken");
        return RedirectToAction(nameof(Login));
    }
}
