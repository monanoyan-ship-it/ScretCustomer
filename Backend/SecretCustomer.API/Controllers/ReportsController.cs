using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin,QualitySpecialist,Inspector")]
public class ReportsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Penalties()
    {
        return View();
    }

    /// <summary>
    /// Temsilci Karnesi - Personnel Report Card
    /// </summary>
    public IActionResult PersonnelReportCard(Guid? personnelId = null)
    {
        ViewBag.SelectedPersonnelId = personnelId;
        return View();
    }

    /// <summary>
    /// Şube Karnesi - Dealer Report Card
    /// </summary>
    public IActionResult DealerReportCard()
    {
        return View();
    }

    /// <summary>
    /// Öneriler Raporu - Suggestions Report
    /// </summary>
    public IActionResult Suggestions()
    {
        return View();
    }

    /// <summary>
    /// Anket Sonuçları - Online Survey Results
    /// </summary>
    public IActionResult SurveyResults()
    {
        return View();
    }

    /// <summary>
    /// Performans Takibi - Dinleyici performansları ve firma kota durumları (Admin Only)
    /// </summary>
    [Authorize(Roles = "Admin")]
    public IActionResult PerformanceTracking()
    {
        return View();
    }

    /// <summary>
    /// Performansım - QualitySpecialist kendi performansını görür
    /// </summary>
    public IActionResult MyPerformance()
    {
        return View();
    }

    /// <summary>
    /// Personel Soru Bazlı Performans Raporu
    /// </summary>
    public IActionResult PersonnelQuestionPerformance()
    {
        return View();
    }

    /// <summary>
    /// AI Destekli Rapor Oluşturma (Gemini API)
    /// </summary>
    [Authorize(Roles = "Admin")]
    public IActionResult AIReport()
    {
        return View();
    }

    /// <summary>
    /// Enneagram Kişilik Testi Sonuçları
    /// </summary>
    public IActionResult EnneagramResults()
    {
        return View();
    }

    /// <summary>
    /// Personel Değerlendirme Raporu (180°/360°)
    /// </summary>
    [Authorize(Roles = "Admin")]
    public IActionResult AssessmentReport()
    {
        return View();
    }
}
