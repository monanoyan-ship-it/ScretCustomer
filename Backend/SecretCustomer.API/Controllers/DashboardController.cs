using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ISystemSettingService _systemSettingService;

    public DashboardController(ISystemSettingService systemSettingService)
    {
        _systemSettingService = systemSettingService;
    }

    public async Task<IActionResult> Index()
    {
        // FieldWorker kullanıcılarını kendi dashboard'una yönlendir
        if (User.IsInRole("FieldWorker"))
        {
            return RedirectToAction("Index", "FieldWorker");
        }

        // Müşteri personelini CustomerPortal dashboard'una yönlendir
        if (User.IsInRole("CustomerManager") || User.IsInRole("CustomerSupervisor") ||
            User.IsInRole("CustomerOperator"))
        {
            return RedirectToAction("Dashboard", "CustomerPortal");
        }

        // AppUrl'i kaydet
        var appUrl = $"{Request.Scheme}://{Request.Host}";
        await _systemSettingService.SetValueAsync(SystemSettingKeys.AppUrl, appUrl);

        return View();
    }

    /// <summary>
    /// Müşteri personeli için basit dashboard
    /// </summary>
    [Authorize(Roles = "CustomerManager,CustomerSupervisor,CustomerOperator")]
    public IActionResult MyDashboard()
    {
        return View();
    }
}
