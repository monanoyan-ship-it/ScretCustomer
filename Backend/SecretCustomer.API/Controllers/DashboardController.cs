using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize]
public class DashboardController : Controller
{
    public IActionResult Index()
    {
        // FieldWorker kullanıcılarını Görevlerim sayfasına yönlendir
        if (User.IsInRole("FieldWorker"))
        {
            return RedirectToAction("Index", "MyAssignments");
        }

        return View();
    }
}
