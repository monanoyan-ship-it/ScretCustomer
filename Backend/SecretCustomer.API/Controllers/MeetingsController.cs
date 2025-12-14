using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Toplantı yönetimi MVC controller
/// </summary>
[Authorize(Roles = "Admin,TeamLeader")]
public class MeetingsController : Controller
{
    /// <summary>
    /// Toplantı yönetimi sayfası (Liste + Create/Edit/Detail modalleri)
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }
}
