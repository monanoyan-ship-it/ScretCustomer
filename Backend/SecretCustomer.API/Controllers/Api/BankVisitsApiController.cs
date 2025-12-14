using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.BankVisit;
using SecretCustomer.Core.Interfaces.Services;
using ClosedXML.Excel;

namespace SecretCustomer.API.Controllers.Api;

/// <summary>
/// Banka Gizli Müşteri Ziyaretleri API (GBF - Gizli Banka Formu)
/// </summary>
[ApiController]
[Route("api/bank-visits")]
[Authorize]
public class BankVisitsApiController : ControllerBase
{
    private readonly IBankVisitService _bankVisitService;
    private readonly ILogger<BankVisitsApiController> _logger;

    public BankVisitsApiController(
        IBankVisitService bankVisitService,
        ILogger<BankVisitsApiController> logger)
    {
        _bankVisitService = bankVisitService;
        _logger = logger;
    }

    /// <summary>
    /// Banka ziyaretlerini listele
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] BankVisitFilterDto filter)
    {
        try
        {
            var visits = await _bankVisitService.GetFilteredAsync(filter);
            return Ok(visits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank visits");
            return StatusCode(500, new { message = "Banka ziyaretleri yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Tek banka ziyareti detayı
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        try
        {
            var visit = await _bankVisitService.GetByIdAsync(id);
            if (visit == null)
                return NotFound(new { message = "Banka ziyareti bulunamadı." });

            return Ok(visit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank visit {Id}", id);
            return StatusCode(500, new { message = "Banka ziyareti yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// CustomerVisit ID'ye göre banka ziyaret detayını getir
    /// </summary>
    [HttpGet("by-customer-visit/{customerVisitId}")]
    public async Task<IActionResult> GetByCustomerVisitId(Guid customerVisitId)
    {
        try
        {
            var visit = await _bankVisitService.GetByCustomerVisitIdAsync(customerVisitId);
            if (visit == null)
                return NotFound(new { message = "Bu ziyaret için banka detayı bulunamadı." });

            return Ok(visit);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank visit for customer visit {CustomerVisitId}", customerVisitId);
            return StatusCode(500, new { message = "Banka ziyareti yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Müşteriye ait banka ziyaretlerini getir
    /// </summary>
    [HttpGet("by-customer/{customerId}")]
    public async Task<IActionResult> GetByCustomerId(Guid customerId)
    {
        try
        {
            var visits = await _bankVisitService.GetByCustomerIdAsync(customerId);
            return Ok(visits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank visits for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Banka ziyaretleri yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Şubeye ait banka ziyaretlerini getir
    /// </summary>
    [HttpGet("by-branch/{branchId}")]
    public async Task<IActionResult> GetByBranchId(Guid branchId)
    {
        try
        {
            var visits = await _bankVisitService.GetByBranchIdAsync(branchId);
            return Ok(visits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank visits for branch {BranchId}", branchId);
            return StatusCode(500, new { message = "Banka ziyaretleri yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Yeni banka ziyaret detayı oluştur
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,TeamLeader,CustomerRepresentative")]
    public async Task<IActionResult> Create([FromBody] CreateBankVisitDetailsDto dto)
    {
        try
        {
            var visit = await _bankVisitService.CreateAsync(dto);
            _logger.LogInformation("Bank visit {Id} created for customer visit {CustomerVisitId}", visit.Id, dto.CustomerVisitId);
            return Ok(visit);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating bank visit");
            return StatusCode(500, new { message = "Banka ziyareti oluşturulurken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Banka ziyaret detayını güncelle
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,TeamLeader,CustomerRepresentative")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBankVisitDetailsDto dto)
    {
        try
        {
            var visit = await _bankVisitService.UpdateAsync(id, dto);
            _logger.LogInformation("Bank visit {Id} updated", id);
            return Ok(visit);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating bank visit {Id}", id);
            return StatusCode(500, new { message = "Banka ziyareti güncellenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Banka ziyaret detayını sil
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,TeamLeader")]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _bankVisitService.DeleteAsync(id);
            _logger.LogInformation("Bank visit {Id} deleted", id);
            return Ok(new { message = "Banka ziyareti silindi." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting bank visit {Id}", id);
            return StatusCode(500, new { message = "Banka ziyareti silinirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Banka ziyareti istatistiklerini getir
    /// </summary>
    [HttpGet("statistics")]
    public async Task<IActionResult> GetStatistics([FromQuery] BankVisitFilterDto? filter)
    {
        try
        {
            var stats = await _bankVisitService.GetStatisticsAsync(filter);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting bank visit statistics");
            return StatusCode(500, new { message = "İstatistikler yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Şube bazlı istatistikleri getir
    /// </summary>
    [HttpGet("branch-statistics/{customerId}")]
    public async Task<IActionResult> GetBranchStatistics(Guid customerId, [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        try
        {
            var stats = await _bankVisitService.GetBranchStatisticsAsync(customerId, fromDate, toDate);
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting branch statistics for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Şube istatistikleri yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Excel'e aktar
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] BankVisitFilterDto? filter)
    {
        try
        {
            var visits = await _bankVisitService.GetForExportAsync(filter);
            var visitList = visits.ToList();

            if (!visitList.Any())
            {
                return NotFound(new { message = "Dışa aktarılacak veri bulunamadı." });
            }

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Banka Ziyaretleri");

            // Headers
            var headers = new[]
            {
                "Senaryo", "Senaryo Tamamlandı", "Giriş Zamanı", "Çıkış Zamanı",
                "Sıra Bekleme (dk)", "Hizmet Süresi (dk)", "Personel Adı",
                "Karşılama", "Vedalaşma", "Personel Görünüm", "Personel Bilgi",
                "Personel İlgi", "Personel İletişim", "Giriş Alanı", "ATM Alanı",
                "Bekleme Alanı", "Gişe Alanı", "Temizlik", "Aydınlatma",
                "Genel Memnuniyet", "Tavsiye Skoru", "Güçlü Yönler", "Gelişim Alanları"
            };

            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cell(1, i + 1).Value = headers[i];
                worksheet.Cell(1, i + 1).Style.Font.Bold = true;
                worksheet.Cell(1, i + 1).Style.Fill.BackgroundColor = XLColor.LightBlue;
            }

            // Data
            int row = 2;
            foreach (var visit in visitList)
            {
                worksheet.Cell(row, 1).Value = visit.ScenarioName;
                worksheet.Cell(row, 2).Value = visit.ScenarioCompleted ? "Evet" : "Hayır";
                worksheet.Cell(row, 3).Value = visit.EntryTime?.ToString("dd.MM.yyyy HH:mm") ?? "";
                worksheet.Cell(row, 4).Value = visit.ExitTime?.ToString("dd.MM.yyyy HH:mm") ?? "";
                worksheet.Cell(row, 5).Value = visit.QueueWaitMinutes ?? 0;
                worksheet.Cell(row, 6).Value = visit.ServiceDurationMinutes ?? 0;
                worksheet.Cell(row, 7).Value = visit.StaffName ?? "";
                worksheet.Cell(row, 8).Value = visit.GreetingReceived ? "Evet" : "Hayır";
                worksheet.Cell(row, 9).Value = visit.FarewellReceived ? "Evet" : "Hayır";
                worksheet.Cell(row, 10).Value = visit.StaffAppearanceRating ?? 0;
                worksheet.Cell(row, 11).Value = visit.StaffKnowledgeRating ?? 0;
                worksheet.Cell(row, 12).Value = visit.StaffAttentivenessRating ?? 0;
                worksheet.Cell(row, 13).Value = visit.StaffCommunicationRating ?? 0;
                worksheet.Cell(row, 14).Value = visit.EntranceAreaRating ?? 0;
                worksheet.Cell(row, 15).Value = visit.AtmAreaRating ?? 0;
                worksheet.Cell(row, 16).Value = visit.WaitingAreaRating ?? 0;
                worksheet.Cell(row, 17).Value = visit.CounterAreaRating ?? 0;
                worksheet.Cell(row, 18).Value = visit.CleanlinessRating ?? 0;
                worksheet.Cell(row, 19).Value = visit.LightingRating ?? 0;
                worksheet.Cell(row, 20).Value = visit.OverallSatisfactionRating ?? 0;
                worksheet.Cell(row, 21).Value = visit.RecommendationScore ?? 0;
                worksheet.Cell(row, 22).Value = visit.Strengths ?? "";
                worksheet.Cell(row, 23).Value = visit.ImprovementAreas ?? "";
                row++;
            }

            worksheet.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"Banka_Ziyaretleri_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting bank visits");
            return StatusCode(500, new { message = "Excel dışa aktarılırken bir hata oluştu." });
        }
    }
}
