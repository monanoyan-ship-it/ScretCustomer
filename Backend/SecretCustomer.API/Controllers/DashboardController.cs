using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Data;

namespace SecretCustomer.API.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly ApplicationDbContext _context;

    public DashboardController(ApplicationDbContext context)
    {
        _context = context;
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
        await SaveAppUrlAsync();

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

    private async Task SaveAppUrlAsync()
    {
        var appUrl = $"{Request.Scheme}://{Request.Host}";
        var setting = await _context.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == SystemSettingKeys.AppUrl);

        if (setting == null)
        {
            _context.SystemSettings.Add(new SystemSetting
            {
                Key = SystemSettingKeys.AppUrl,
                Value = appUrl,
                Description = "Uygulamanın çalıştığı URL (otomatik set edilir)",
                ValueType = "string",
                Category = "System"
            });
        }
        else if (setting.Value != appUrl)
        {
            setting.Value = appUrl;
        }

        await _context.SaveChangesAsync();
    }
}
