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

        // Müşteri personelini CustomerPortal dashboard'una yönlendir
        if (User.IsInRole("CustomerManager") || User.IsInRole("CustomerSupervisor") ||
            User.IsInRole("CustomerOperator") || User.IsInRole("CustomerViewer"))
        {
            return RedirectToAction("Dashboard", "CustomerPortal");
        }

        return View();
    }

    /// <summary>
    /// Müşteri personeli için basit dashboard
    /// </summary>
    [Authorize(Roles = "CustomerManager,CustomerSupervisor,CustomerOperator,CustomerViewer")]
    public IActionResult MyDashboard()
    {
        return View();
    }
}
