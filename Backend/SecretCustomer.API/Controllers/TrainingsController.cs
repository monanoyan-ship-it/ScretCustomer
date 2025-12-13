using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Eğitim yönetimi MVC controller
/// </summary>
[Authorize]
public class TrainingsController : Controller
{
    /// <summary>
    /// Eğitim yönetimi sayfası (Liste + Create/Edit/Detail modalleri)
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }
}
