using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Online Anket Formu - Public erişimli
/// </summary>
[AllowAnonymous]
public class AnketController : Controller
{
    /// <summary>
    /// Anket formu sayfası
    /// </summary>
    [HttpGet]
    public IActionResult Form(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return View("Error", new AnketErrorViewModel
            {
                Title = "Geçersiz Link",
                Message = "Anket linki geçersiz. Lütfen size gönderilen linki kontrol edin."
            });
        }

        ViewBag.Token = token;
        return View();
    }

    /// <summary>
    /// Anket tamamlandı sayfası
    /// </summary>
    [HttpGet]
    public IActionResult Complete()
    {
        return View();
    }

    /// <summary>
    /// Hata sayfası
    /// </summary>
    [HttpGet]
    public IActionResult Error(string? title, string? message)
    {
        return View(new AnketErrorViewModel
        {
            Title = title ?? "Hata",
            Message = message ?? "Bir hata oluştu."
        });
    }
}

public class AnketErrorViewModel
{
    public string Title { get; set; } = "Hata";
    public string Message { get; set; } = "Bir hata oluştu.";
}
