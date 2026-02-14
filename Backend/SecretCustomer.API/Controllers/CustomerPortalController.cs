using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Data;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Müşteri Portalı - MVC Controller
/// Müşteri personelinin giriş yapıp değerlendirme sonuçlarını görüntüleyeceği portal
/// </summary>
[Authorize(Roles = "Admin,CustomerManager,CustomerSupervisor,CustomerOperator")]
public class CustomerPortalController : Controller
{
    private readonly ApplicationDbContext _context;

    public CustomerPortalController(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Müşteri portalı dashboard
    /// </summary>
    public async Task<IActionResult> Dashboard()
    {
        // AppUrl'i kaydet
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
            await _context.SaveChangesAsync();
        }
        else if (setting.Value != appUrl)
        {
            setting.Value = appUrl;
            await _context.SaveChangesAsync();
        }

        return View();
    }

    /// <summary>
    /// Değerlendirme sonuçları
    /// </summary>
    public IActionResult Evaluations()
    {
        return View();
    }

    /// <summary>
    /// Raporlar
    /// </summary>
    public IActionResult Reports()
    {
        return View();
    }

    /// <summary>
    /// İç Dinleme Raporları - firma personeli tarafından yapılan değerlendirmeler
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager,CustomerSupervisor,Admin")]
    public IActionResult InternalReports()
    {
        return View();
    }

    /// <summary>
    /// Dış Dinleme Raporları - bizim tarafımızdan yapılan değerlendirmeler
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager,CustomerSupervisor,Admin")]
    public IActionResult ExternalReports()
    {
        return View();
    }

    /// <summary>
    /// Cezalı KL Raporu
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager,CustomerSupervisor,Admin")]
    public IActionResult Penalties()
    {
        return View();
    }

    /// <summary>
    /// Öneriler Raporu
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager,CustomerSupervisor,Admin")]
    public IActionResult Suggestions()
    {
        return View();
    }

    /// <summary>
    /// Temsilci Karnesi
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager,CustomerSupervisor,Admin")]
    public IActionResult PersonnelReportCard()
    {
        return View();
    }

    /// <summary>
    /// Şube Karnesi - Dealer Report Card
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager,CustomerSupervisor,Admin")]
    public IActionResult DealerReportCard()
    {
        return View();
    }

    /// <summary>
    /// Dönemlere Göre Personel Başarı Tablosu
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager,CustomerSupervisor,Admin")]
    public IActionResult PerformanceByPeriod()
    {
        return View();
    }

    /// <summary>
    /// Proje bazlı sonuçlar (sadece CustomerManager ve Admin)
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager,Admin")]
    public IActionResult Projects()
    {
        return View();
    }

    /// <summary>
    /// Organizasyonlar/Şubeler (sadece CustomerManager ve Admin)
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager,Admin")]
    public IActionResult Organizations()
    {
        return View();
    }

    /// <summary>
    /// Süpervizörler (sadece CustomerManager ve Admin)
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager,Admin")]
    public IActionResult Supervisors()
    {
        return View();
    }

    /// <summary>
    /// İç dinlemeler - firma personeli tarafından yapılan
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager,CustomerSupervisor,Admin")]
    public IActionResult InternalEvaluations()
    {
        return View();
    }

    /// <summary>
    /// Dış dinlemeler - bizim tarafımızdan yapılan
    /// </summary>
    [Microsoft.AspNetCore.Authorization.Authorize(Roles = "CustomerManager,CustomerSupervisor,Admin")]
    public IActionResult ExternalEvaluations()
    {
        return View();
    }

    /// <summary>
    /// Kullanıcı profili ve şifre değiştirme
    /// </summary>
    public IActionResult Profile()
    {
        return View();
    }

    /// <summary>
    /// Kullanıcının kendi eğitim videoları
    /// </summary>
    [Route("CustomerPortal/MyTrainings")]
    public IActionResult MyTrainings()
    {
        return View();
    }

    /// <summary>
    /// Temsilcinin kendi performansı - dinlenen çağrıları, puanları, yorumları
    /// Sadece CustomerOperator rolü için
    /// </summary>
    [Route("CustomerPortal/MyPerformance")]
    [Authorize(Roles = "CustomerOperator")]
    public IActionResult MyPerformance()
    {
        return View();
    }

    /// <summary>
    /// Eğitim videosu anketi - Dahili katılımcılar için
    /// </summary>
    [Route("CustomerPortal/TrainingQuiz/{participantId:int}")]
    public IActionResult TrainingQuiz(int participantId)
    {
        ViewBag.ParticipantId = participantId;
        return View();
    }

    /// <summary>
    /// Personel eğitimleri - Yönetici ve süpervizörler için
    /// </summary>
    [Route("CustomerPortal/StaffTrainings")]
    [Authorize(Roles = "CustomerManager,CustomerSupervisor,Admin")]
    public IActionResult StaffTrainings()
    {
        return View();
    }

    /// <summary>
    /// Anket Sonuçları - Online Survey proje sonuçları
    /// </summary>
    [Route("CustomerPortal/SurveyResults")]
    [Authorize(Roles = "CustomerManager,CustomerSupervisor,Admin")]
    public IActionResult SurveyResults()
    {
        return View();
    }

    /// <summary>
    /// Enneagram Sonuçları - Enneagram kişilik testi sonuçları
    /// </summary>
    [Route("CustomerPortal/EnneagramResults")]
    [Authorize(Roles = "CustomerManager,CustomerSupervisor,Admin")]
    public IActionResult EnneagramResults()
    {
        return View();
    }

    /// <summary>
    /// Puan Eşikleri - Müşteri bazlı başarı/uyarı eşikleri
    /// </summary>
    [Route("CustomerPortal/ScoreThresholds")]
    [Authorize(Roles = "CustomerManager,Admin")]
    public IActionResult ScoreThresholds()
    {
        return View();
    }

    /// <summary>
    /// Gölge Müşteri Aramaları
    /// </summary>
    [Route("CustomerPortal/GolgeMusteriAramalar")]
    [Authorize(Roles = "CustomerManager,CustomerSupervisor,Admin")]
    public IActionResult GolgeMusteriAramalar()
    {
        return View();
    }
}
