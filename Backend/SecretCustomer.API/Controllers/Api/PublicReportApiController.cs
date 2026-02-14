using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Interfaces.Services;

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
    private readonly ILogger<PublicReportApiController> _logger;

    public PublicReportApiController(
        INotificationTokenService tokenService,
        IReportService reportService,
        ILogger<PublicReportApiController> logger)
    {
        _tokenService = tokenService;
        _reportService = reportService;
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
            var authInfo = await _reportService.GetEvaluationAuthInfoAsync(evaluationId);
            if (authInfo == null)
                return NotFound(new { message = "Değerlendirme bulunamadı." });

            // Yetki kontrolü
            switch (payload.Type)
            {
                case "single":
                    if (authInfo.Value.EvaluationId != payload.EvaluationId)
                        return Forbid();
                    break;

                case "bulk":
                    if (authInfo.Value.ProjectCustomerId != payload.CustomerId)
                        return Forbid();
                    break;

                case "personnel":
                    if (authInfo.Value.EvaluatedCustomerPersonnelId != payload.CustomerPersonnelId)
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
        var result = await _reportService.GetBulkPublicReportAsync(customerId, startDate, endDate);
        if (result == null)
            return NotFound(new { message = "Müşteri bulunamadı." });
        return Ok(result);
    }

    private async Task<IActionResult> GetPersonnelReport(int customerPersonnelId, DateTime startDate, DateTime endDate)
    {
        var result = await _reportService.GetPersonnelPublicReportAsync(customerPersonnelId, startDate, endDate);
        if (result == null)
            return NotFound(new { message = "Personel bulunamadı." });
        return Ok(result);
    }
}
