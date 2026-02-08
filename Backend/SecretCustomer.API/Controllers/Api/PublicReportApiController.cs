using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.API.Controllers.Api;

/// <summary>
/// Public API - Token ile erişilen değerlendirme raporları.
/// Authentication gerektirmez.
/// </summary>
[ApiController]
[Route("api/public/report")]
[AllowAnonymous]
public class PublicReportApiController : ControllerBase
{
    private readonly INotificationTokenService _tokenService;
    private readonly IReportService _reportService;
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PublicReportApiController> _logger;

    public PublicReportApiController(
        INotificationTokenService tokenService,
        IReportService reportService,
        ApplicationDbContext context,
        ILogger<PublicReportApiController> logger)
    {
        _tokenService = tokenService;
        _reportService = reportService;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Token'a göre rapor verisini döndürür.
    /// type=single → tek değerlendirme detayı
    /// type=bulk → müşterinin tarih aralığındaki değerlendirme listesi
    /// type=personnel → personelin tarih aralığındaki değerlendirme listesi
    /// </summary>
    [HttpGet("{token}")]
    public async Task<IActionResult> GetReport(string token)
    {
        var payload = _tokenService.DecryptToken(token);
        if (payload == null)
            return Unauthorized(new { message = "Geçersiz veya süresi dolmuş link." });

        try
        {
            switch (payload.Type)
            {
                case "single":
                    return await GetSingleReport(payload.EvaluationId!.Value);

                case "bulk":
                    return await GetBulkReport(payload.CustomerId!.Value, payload.StartDate!.Value, payload.EndDate!.Value);

                case "personnel":
                    return await GetPersonnelReport(payload.CustomerPersonnelId!.Value, payload.StartDate!.Value, payload.EndDate!.Value);

                default:
                    return BadRequest(new { message = "Bilinmeyen rapor tipi." });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public report for token type {Type}", payload.Type);
            return StatusCode(500, new { message = "Rapor yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Bulk/Personnel listesinden tek bir değerlendirmenin detayını getirir.
    /// Token geçerliliği + ait olma kontrolü yapılır.
    /// </summary>
    [HttpGet("{token}/evaluation/{evaluationId}")]
    public async Task<IActionResult> GetEvaluationDetail(string token, int evaluationId)
    {
        var payload = _tokenService.DecryptToken(token);
        if (payload == null)
            return Unauthorized(new { message = "Geçersiz veya süresi dolmuş link." });

        try
        {
            // Token sahibinin bu değerlendirmeyi görme yetkisi var mı kontrol et
            var evaluation = await _context.Evaluations
                .Include(e => e.Project)
                .Include(e => e.EvaluatedCustomerPersonnel)
                .FirstOrDefaultAsync(e => e.Id == evaluationId && !e.IsDeleted);

            if (evaluation == null)
                return NotFound(new { message = "Değerlendirme bulunamadı." });

            // Yetki kontrolü
            switch (payload.Type)
            {
                case "single":
                    if (evaluation.Id != payload.EvaluationId)
                        return Forbid();
                    break;

                case "bulk":
                    if (evaluation.Project?.CustomerId != payload.CustomerId)
                        return Forbid();
                    break;

                case "personnel":
                    if (evaluation.EvaluatedCustomerPersonnelId != payload.CustomerPersonnelId)
                        return Forbid();
                    break;
            }

            var detail = await _reportService.GetEvaluationDetailAsync(evaluationId);
            if (detail == null)
                return NotFound(new { message = "Değerlendirme detayı bulunamadı." });

            return Ok(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting public evaluation detail {EvaluationId}", evaluationId);
            return StatusCode(500, new { message = "Değerlendirme detayı yüklenirken hata oluştu." });
        }
    }

    private async Task<IActionResult> GetSingleReport(int evaluationId)
    {
        var detail = await _reportService.GetEvaluationDetailAsync(evaluationId);
        if (detail == null)
            return NotFound(new { message = "Değerlendirme bulunamadı." });

        return Ok(new
        {
            type = "single",
            evaluation = detail
        });
    }

    private async Task<IActionResult> GetBulkReport(int customerId, DateTime startDate, DateTime endDate)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == customerId && !c.IsDeleted);

        if (customer == null)
            return NotFound(new { message = "Müşteri bulunamadı." });

        var evaluations = await _context.Evaluations
            .Include(e => e.Project)
            .Include(e => e.EvaluatedCustomerPersonnel)
            .Include(e => e.CustomerDealer)
            .Where(e => !e.IsDeleted
                && e.Project != null
                && e.Project.CustomerId == customerId
                && e.CompletedAt != null
                && e.CompletedAt >= startDate
                && e.CompletedAt < endDate)
            .OrderByDescending(e => e.CompletedAt)
            .Select(e => new
            {
                e.Id,
                e.CompletedAt,
                e.TotalScore,
                e.MaxScore,
                ScorePercentage = (e.MaxScore ?? 0) > 0 ? Math.Round((decimal)(e.TotalScore ?? 0) / (decimal)e.MaxScore!.Value * 100, 1) : 0,
                PersonnelName = e.EvaluatedCustomerPersonnel != null
                    ? e.EvaluatedCustomerPersonnel.FirstName + " " + e.EvaluatedCustomerPersonnel.LastName
                    : e.EvaluatedUnknownPersonnel ?? "-",
                DealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : null,
                ProjectName = e.Project!.Name,
                e.EvaluationComment
            })
            .ToListAsync();

        return Ok(new
        {
            type = "bulk",
            customerName = customer.CompanyName,
            startDate,
            endDate,
            evaluations
        });
    }

    private async Task<IActionResult> GetPersonnelReport(int customerPersonnelId, DateTime startDate, DateTime endDate)
    {
        var personnel = await _context.CustomerPersonnel
            .FirstOrDefaultAsync(p => p.Id == customerPersonnelId && !p.IsDeleted);

        if (personnel == null)
            return NotFound(new { message = "Personel bulunamadı." });

        var evaluations = await _context.Evaluations
            .Include(e => e.Project)
            .Include(e => e.CustomerDealer)
            .Where(e => !e.IsDeleted
                && e.EvaluatedCustomerPersonnelId == customerPersonnelId
                && e.CompletedAt != null
                && e.CompletedAt >= startDate
                && e.CompletedAt < endDate)
            .OrderByDescending(e => e.CompletedAt)
            .Select(e => new
            {
                e.Id,
                e.CompletedAt,
                e.TotalScore,
                e.MaxScore,
                ScorePercentage = (e.MaxScore ?? 0) > 0 ? Math.Round((decimal)(e.TotalScore ?? 0) / (decimal)e.MaxScore!.Value * 100, 1) : 0,
                DealerName = e.CustomerDealer != null ? e.CustomerDealer.Name : null,
                ProjectName = e.Project!.Name,
                e.EvaluationComment
            })
            .ToListAsync();

        return Ok(new
        {
            type = "personnel",
            personnelName = personnel.FirstName + " " + personnel.LastName,
            startDate,
            endDate,
            evaluations
        });
    }
}
