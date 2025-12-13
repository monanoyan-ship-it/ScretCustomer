using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Çağrı yönetimi MVC controller
/// </summary>
[Authorize]
public class CallsController : Controller
{
    /// <summary>
    /// Çağrı listesi sayfası (Create/Edit/Detail modal ile)
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }
}
