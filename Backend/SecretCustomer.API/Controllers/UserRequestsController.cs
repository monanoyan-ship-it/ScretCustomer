using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Kullanıcı Talepleri sayfası controller
/// Admin tarafından taslağa alma ve personel taleplerini yönetmek için kullanılır
/// </summary>
[Authorize(Roles = "Admin")]
public class UserRequestsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}
