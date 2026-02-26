using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize]
public class GolgeMusteriController : Controller
{
    [Authorize(Roles = "Admin")]
    public IActionResult HedefFirmalar()
    {
        return View();
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Sorular()
    {
        return View();
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Donemler()
    {
        return View();
    }

    [Authorize(Roles = "Admin,Inspector")]
    public IActionResult Takip()
    {
        return View();
    }

    public IActionResult Aramalarim()
    {
        return View();
    }

    public IActionResult PopupGmDinleme(int? gmAtamaId, int? dinlemeId)
    {
        ViewBag.GmAtamaId = gmAtamaId;
        ViewBag.DinlemeId = dinlemeId;
        return View();
    }
}
