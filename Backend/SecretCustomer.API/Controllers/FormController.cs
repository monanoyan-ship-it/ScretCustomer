using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Public form controller - QR kod ile erişilen değerlendirme formları
/// </summary>
public class FormController : Controller
{
    /// <summary>
    /// Public değerlendirme formu sayfası
    /// UniqueLink ile erişilir (QR kod taraması)
    /// </summary>
    public IActionResult Index(int id)
    {
        ViewBag.UniqueLink = id;
        return View();
    }
}
