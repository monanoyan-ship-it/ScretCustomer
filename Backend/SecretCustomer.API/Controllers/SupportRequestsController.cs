using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Destek Talepleri Yönetimi - Admin için
/// </summary>
[Authorize(Roles = "Admin")]
public class SupportRequestsController : Controller
{
    /// <summary>
    /// Destek talepleri listesi ve yönetimi
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }
}
