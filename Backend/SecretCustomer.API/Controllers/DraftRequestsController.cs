using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Taslağa Alma Talepleri sayfası controller
/// Admin tarafından değerlendirme taslağa alma taleplerini yönetmek için kullanılır
/// </summary>
[Authorize(Roles = "Admin")]
public class DraftRequestsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
