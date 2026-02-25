using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Dashboard;
using SecretCustomer.Core.DTOs.Report;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/customer/portal")]
[AllowAnonymous] // JWT middleware sorunlu, token'ı kendimiz parse ediyoruz
public class CustomerPortalApiController : ControllerBase
{
    private readonly ILogger<CustomerPortalApiController> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly IReportService _reportService;
    private readonly IEvaluationService _evaluationService;
    private readonly ICustomerScoreThresholdService _customerScoreThresholdService;
    private readonly IPdfService _pdfService;
    private readonly ICustomerPortalReportService _cpReportService;
    private readonly ICustomerPortalDataService _cpDataService;

    public CustomerPortalApiController(
        ILogger<CustomerPortalApiController> logger,
        ILocalizationService localizationService,
        IReportService reportService,
        IEvaluationService evaluationService,
        ICustomerScoreThresholdService customerScoreThresholdService,
        IPdfService pdfService,
        ICustomerPortalReportService cpReportService,
        ICustomerPortalDataService cpDataService)
    {
        _logger = logger;
        _localizationService = localizationService;
        _reportService = reportService;
        _evaluationService = evaluationService;
        _customerScoreThresholdService = customerScoreThresholdService;
        _pdfService = pdfService;
        _cpReportService = cpReportService;
        _cpDataService = cpDataService;
    }

    private int? GetCustomerIdFromToken()
    {
        // Authorization header'dan token'ı al
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            _logger.LogWarning("[CustomerPortal] No Authorization header or invalid format");
            return null;
        }

        var token = authHeader.Substring("Bearer ".Length);

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            var customerIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "CustomerId")?.Value;

            _logger.LogInformation("[CustomerPortal] Token parsed. CustomerId: {CustomerId}", customerIdClaim);

            if (int.TryParse(customerIdClaim, out var customerId))
                return customerId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error parsing token");
        }

        return null;
    }

    private int? GetCustomerId()
    {
        // Önce User claims'den dene
        var customerIdClaim = User.FindFirst("CustomerId")?.Value;
        if (int.TryParse(customerIdClaim, out var customerId))
            return customerId;

        // Admin için session'dan al
        if (IsAdmin())
        {
            var sessionCustomerId = HttpContext.Session.GetInt32("AdminViewAsCustomerId");
            if (sessionCustomerId.HasValue)
                return sessionCustomerId.Value;
        }

        // Yoksa token'dan manuel parse et
        return GetCustomerIdFromToken();
    }

    /// <summary>
    /// Kullanıcı Admin mi kontrol eder
    /// </summary>
    private bool IsAdmin()
    {
        if (User.IsInRole("Admin"))
            return true;

        // Token'dan kontrol et
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            try
            {
                var token = authHeader.Substring("Bearer ".Length);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken.Claims.Any(c => c.Type == ClaimTypes.Role && c.Value == "Admin");
            }
            catch { }
        }
        return false;
    }

    // ==================== ADMIN MÜŞTERİ SEÇİM ====================

    /// <summary>
    /// Admin için müşteri listesi (arama destekli)
    /// </summary>
    [HttpGet("admin/customers")]
    public async Task<IActionResult> GetCustomersForAdmin([FromQuery] string? search, [FromQuery] bool includeInactive = true)
    {
        if (!IsAdmin())
            return Forbid();

        var customers = await _cpDataService.GetCustomersForAdminAsync(search, includeInactive);
        return Ok(customers);
    }

    /// <summary>
    /// Admin müşteri seçimi yapar (session'a yazar)
    /// </summary>
    [HttpPost("admin/view-as-customer/{customerId}")]
    public async Task<IActionResult> SetViewAsCustomer(int customerId)
    {
        if (!IsAdmin())
            return Forbid();

        var customer = await _cpDataService.GetCustomerByIdAsync(customerId);

        if (customer == null)
            return NotFound(new { message = "Müşteri bulunamadı." });

        HttpContext.Session.SetInt32("AdminViewAsCustomerId", customerId);
        HttpContext.Session.SetString("AdminViewAsCustomerName", customer.Value.CompanyName);

        return Ok(new {
            success = true,
            customerId = customer.Value.Id,
            customerName = customer.Value.CompanyName
        });
    }

    /// <summary>
    /// Admin'in şu an seçili müşterisini döndürür
    /// </summary>
    [HttpGet("admin/current-customer")]
    public IActionResult GetCurrentCustomer()
    {
        if (!IsAdmin())
            return Forbid();

        var customerId = HttpContext.Session.GetInt32("AdminViewAsCustomerId");
        var customerName = HttpContext.Session.GetString("AdminViewAsCustomerName");

        if (!customerId.HasValue)
            return Ok(new { hasSelection = false });

        return Ok(new {
            hasSelection = true,
            customerId = customerId.Value,
            customerName = customerName
        });
    }

    /// <summary>
    /// Admin müşteri seçimini temizler
    /// </summary>
    [HttpDelete("admin/view-as-customer")]
    public IActionResult ClearViewAsCustomer()
    {
        if (!IsAdmin())
            return Forbid();

        HttpContext.Session.Remove("AdminViewAsCustomerId");
        HttpContext.Session.Remove("AdminViewAsCustomerName");

        return Ok(new { success = true });
    }

    private bool IsCustomerPersonnel()
    {
        if (User.FindFirst("UserType")?.Value == "CustomerPersonnel")
            return true;

        // Token'dan kontrol et
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            try
            {
                var token = authHeader.Substring("Bearer ".Length);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken.Claims.Any(c => c.Type == "UserType" && c.Value == "CustomerPersonnel");
            }
            catch { }
        }
        return false;
    }

    private int? GetPersonnelId()
    {
        // CustomerPersonnel token'ında ClaimTypes.NameIdentifier kullanılıyor
        // Önce UserType claim'ini kontrol et - CustomerPersonnel mi?
        var userType = User.FindFirst("UserType")?.Value;
        if (userType == "CustomerPersonnel")
        {
            var nameIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(nameIdClaim, out var personnelId))
                return personnelId;
        }

        // Eski yöntem - PersonnelId claim'i (geriye uyumluluk)
        var personnelIdClaim = User.FindFirst("PersonnelId")?.Value;
        if (int.TryParse(personnelIdClaim, out var pId))
            return pId;

        // Token'dan manuel parse et
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            try
            {
                var token = authHeader.Substring("Bearer ".Length);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);

                // UserType kontrolü
                var userTypeClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "UserType")?.Value;
                if (userTypeClaim == "CustomerPersonnel")
                {
                    var nameIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier || c.Type == "nameid")?.Value;
                    if (int.TryParse(nameIdClaim, out var personnelId))
                        return personnelId;
                }

                // Eski yöntem
                var claim = jwtToken.Claims.FirstOrDefault(c => c.Type == "PersonnelId")?.Value;
                if (int.TryParse(claim, out pId))
                    return pId;
            }
            catch { }
        }
        return null;
    }

    private string? GetPersonnelRole()
    {
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!string.IsNullOrEmpty(roleClaim))
            return roleClaim;

        // Token'dan manuel parse et
        var authHeader = Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            try
            {
                var token = authHeader.Substring("Bearer ".Length);
                var handler = new JwtSecurityTokenHandler();
                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role || c.Type == "role")?.Value;
            }
            catch { }
        }
        return null;
    }

    /// <summary>
    /// Rol bazlı personel erişim kontrolü - Organizasyon bazında hibrit mantık
    /// </summary>
    /// <returns>
    /// null = Tüm personeli görebilir (Admin/Manager)
    /// List = Sadece bu ID'lerdeki personeli görebilir
    /// </returns>
    private async Task<List<int>?> GetAllowedPersonnelIdsAsync()
    {
        // Admin session ile giriş
        if (IsAdmin())
            return null;

        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();

        return await _cpDataService.GetAllowedPersonnelIdsAsync(role, personnelId);
    }

    /// <summary>
    /// Rol bazlı organizasyon erişim kontrolü
    /// </summary>
    /// <returns>
    /// null = Tüm organizasyonları görebilir (Admin/Manager veya org'a atanmamış Supervisor)
    /// List = Sadece bu ID'lerdeki organizasyonları görebilir
    /// </returns>
    private async Task<List<int>?> GetAllowedOrganizationIdsAsync()
    {
        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();

        return await _cpDataService.GetAllowedOrganizationIdsAsync(role, personnelId);
    }

    /// <summary>
    /// Dashboard istatistikleri
    /// </summary>
    [HttpGet("dashboard/stats")]
    public async Task<IActionResult> GetDashboardStats()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.GetDashboardStatsAsync(customerId.Value, allowedPersonnelIds);
        return Ok(result);
    }

    /// <summary>
    /// Aylık değerlendirme trendi (son 12 ay)
    /// </summary>
    [HttpGet("dashboard/monthly-trend")]
    public async Task<IActionResult> GetMonthlyTrend([FromQuery] int? projectId = null, [FromQuery] int? projectTypeId = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.GetMonthlyTrendAsync(customerId.Value, allowedPersonnelIds, projectId, projectTypeId, startDate, endDate);
        return Ok(result);
    }

    /// <summary>
    /// Proje tipine göre ayrı aylık değerlendirme trendleri
    /// </summary>
    [HttpGet("dashboard/monthly-trend-by-type")]
    public async Task<IActionResult> GetMonthlyTrendByType([FromQuery] int? projectTypeId = null, [FromQuery] int? projectId = null, [FromQuery] int? checklistTypeId = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.GetMonthlyTrendByTypeAsync(customerId.Value, allowedPersonnelIds, startDate, endDate, projectTypeId, projectId, checklistTypeId);
        return Ok(result);
    }

    /// <summary>
    /// Soru grupları bazlı aylık trend (son 12 ay)
    /// </summary>
    [HttpGet("dashboard/question-group-trend")]
    public async Task<IActionResult> GetQuestionGroupTrend([FromQuery] List<int>? projectIds = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.GetQuestionGroupTrendAsync(customerId.Value, allowedPersonnelIds, projectIds, startDate, endDate);
        return Ok(result);
    }

    /// <summary>
    /// Sorular bazlı aylık trend (son 12 ay)
    /// </summary>
    [HttpGet("dashboard/question-trend")]
    public async Task<IActionResult> GetQuestionTrend(
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] string? groupName = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.GetQuestionTrendAsync(customerId.Value, allowedPersonnelIds, projectIds, groupName, startDate, endDate);
        return Ok(result);
    }

    /// <summary>
    /// Puan dağılımı (proje ve tarih filtreli)
    /// </summary>
    [HttpGet("dashboard/score-distribution")]
    public async Task<IActionResult> GetScoreDistribution([FromQuery] int? projectId, [FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.GetScoreDistributionAsync(customerId.Value, allowedPersonnelIds, projectId, startDate, endDate);
        return Ok(result);
    }

    /// <summary>
    /// Puan dağılımı kategorisindeki değerlendirmeler (tıklanan renge göre)
    /// </summary>
    [HttpGet("dashboard/score-distribution/evaluations")]
    public async Task<IActionResult> GetScoreDistributionEvaluations(
        [FromQuery] string category,
        [FromQuery] int projectTypeId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.GetScoreDistributionEvaluationsAsync(customerId.Value, allowedPersonnelIds, category, projectTypeId, startDate, endDate, page, pageSize);
        if (result == null)
            return BadRequest(new { message = "Geçersiz kategori. Geçerli değerler: success, warning, danger" });

        return Ok(result);
    }

    /// <summary>
    /// Puan dağılımı kategorisindeki değerlendirmeleri Excel'e export et
    /// </summary>
    [HttpGet("dashboard/score-distribution/export")]
    public async Task<IActionResult> ExportScoreDistributionEvaluations(
        [FromQuery] string category,
        [FromQuery] int projectTypeId,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var exportResult = await _cpDataService.ExportScoreDistributionEvaluationsAsync(customerId.Value, allowedPersonnelIds, category, projectTypeId, startDate, endDate);
        if (exportResult == null)
            return BadRequest(new { message = "Geçersiz kategori" });

        return File(exportResult.Value.FileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", exportResult.Value.FileName);
    }

    /// <summary>
    /// Dashboard chart'larini Excel'e export et (chart gorseli + veri tablosu)
    /// </summary>
    [HttpPost("dashboard/charts/export")]
    public IActionResult ExportChartToExcel([FromBody] ChartExportRequestDto dto)
    {
        if (string.IsNullOrEmpty(dto.ChartImage) || string.IsNullOrEmpty(dto.DataJson))
            return BadRequest(new { message = "Chart image ve data gereklidir." });

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Chart");

        var title = dto.ChartTitle ?? dto.ChartType;
        // Row 1: Title (bold, merged)
        worksheet.Cell(1, 1).Value = title;
        worksheet.Cell(1, 1).Style.Font.Bold = true;
        worksheet.Cell(1, 1).Style.Font.FontSize = 14;
        worksheet.Range(1, 1, 1, 6).Merge();

        // Row 2: Date
        worksheet.Cell(2, 1).Value = TurkeyTime.Now.ToString("dd.MM.yyyy HH:mm");
        worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

        // Row 4+: Chart image
        var imageStartRow = 4;
        var imageRowCount = 15; // approximate rows the image will span
        try
        {
            var base64Data = dto.ChartImage;
            if (base64Data.Contains(","))
                base64Data = base64Data.Substring(base64Data.IndexOf(",") + 1);

            var imageBytes = Convert.FromBase64String(base64Data);
            using var imgStream = new MemoryStream(imageBytes);
            var picture = worksheet.AddPicture(imgStream, XLPictureFormat.Png);
            picture.MoveTo(worksheet.Cell(imageStartRow, 1));
            picture.Scale(0.6);

            // Estimate how many rows the image takes
            imageRowCount = (int)Math.Ceiling(picture.Height / 15.0 * 0.6);
            if (imageRowCount < 15) imageRowCount = 15;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chart image embed failed");
            worksheet.Cell(imageStartRow, 1).Value = "(Grafik goruntusu yuklenemedi)";
        }

        // Data table starts after image
        var dataStartRow = imageStartRow + imageRowCount + 2;

        try
        {
            switch (dto.ChartType?.ToLower())
            {
                case "monthly-trend":
                    BuildMonthlyTrendTable(worksheet, dto.DataJson, dataStartRow);
                    break;
                case "score-distribution":
                    BuildScoreDistributionTable(worksheet, dto.DataJson, dataStartRow);
                    break;
                case "question-trend":
                    BuildQuestionTrendTable(worksheet, dto.DataJson, dataStartRow);
                    break;
                default:
                    worksheet.Cell(dataStartRow, 1).Value = "Bilinmeyen chart tipi: " + dto.ChartType;
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Chart data table build failed for type {ChartType}", dto.ChartType);
            worksheet.Cell(dataStartRow, 1).Value = "(Veri tablosu olusturulamadi)";
        }

        worksheet.Columns().AdjustToContents();
        ExcelHelper.ApplyLongTextColumnStyles(worksheet);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"{title.Replace(" ", "_")}_{TurkeyTime.Now:yyyyMMdd_HHmmss}.xlsx";
        return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    private void BuildMonthlyTrendTable(IXLWorksheet ws, string dataJson, int startRow)
    {
        var items = JsonSerializer.Deserialize<List<JsonElement>>(dataJson);
        if (items == null || items.Count == 0) return;

        // Header
        ws.Cell(startRow, 1).Value = "Ay";
        ws.Cell(startRow, 2).Value = "Ort. Puan";
        ws.Cell(startRow, 3).Value = "Degerlendirme";
        ws.Cell(startRow, 4).Value = "Sari Kart";
        ws.Cell(startRow, 5).Value = "Kirmizi Kart";

        var headerRange = ws.Range(startRow, 1, startRow, 5);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        for (int i = 0; i < items.Count; i++)
        {
            var row = startRow + 1 + i;
            var item = items[i];
            ws.Cell(row, 1).Value = item.GetProperty("month").GetString() ?? "";
            ws.Cell(row, 2).Value = item.GetProperty("averageScore").GetDouble();
            ws.Cell(row, 3).Value = item.GetProperty("count").GetInt32();
            ws.Cell(row, 4).Value = GetJsonInt(item, "yellowCardCount");
            ws.Cell(row, 5).Value = GetJsonInt(item, "redCardCount");
        }
    }

    private void BuildScoreDistributionTable(IXLWorksheet ws, string dataJson, int startRow)
    {
        var data = JsonSerializer.Deserialize<JsonElement>(dataJson);

        // Header
        ws.Cell(startRow, 1).Value = "Kategori";
        ws.Cell(startRow, 2).Value = "Adet";

        var headerRange = ws.Range(startRow, 1, startRow, 2);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        var categories = new[] {
            ("Mukemmel (90+)", "excellent"),
            ("Iyi (80-89)", "good"),
            ("Orta (60-79)", "average"),
            ("Dusuk (<60)", "poor")
        };

        for (int i = 0; i < categories.Length; i++)
        {
            var row = startRow + 1 + i;
            ws.Cell(row, 1).Value = categories[i].Item1;
            ws.Cell(row, 2).Value = GetJsonInt(data, categories[i].Item2);
        }
    }

    private void BuildQuestionTrendTable(IXLWorksheet ws, string dataJson, int startRow)
    {
        var data = JsonSerializer.Deserialize<JsonElement>(dataJson);
        if (data.ValueKind != JsonValueKind.Object) return;

        var monthLabels = new List<string>();
        if (data.TryGetProperty("monthLabels", out var labelsEl) && labelsEl.ValueKind == JsonValueKind.Array)
        {
            foreach (var label in labelsEl.EnumerateArray())
                monthLabels.Add(label.GetString() ?? "");
        }

        // Determine which property holds trends
        JsonElement trendsEl;
        if (data.TryGetProperty("groupTrends", out trendsEl) || data.TryGetProperty("questionTrends", out trendsEl))
        {
            // ok
        }
        else
        {
            return;
        }

        if (trendsEl.ValueKind != JsonValueKind.Array) return;
        var trends = trendsEl.EnumerateArray().ToList();
        if (trends.Count == 0) return;

        // Header row: "Soru/Grup", Month1, Month2, ...
        ws.Cell(startRow, 1).Value = "Soru / Grup";
        for (int j = 0; j < monthLabels.Count; j++)
        {
            ws.Cell(startRow, 2 + j).Value = monthLabels[j];
        }

        var colCount = 1 + monthLabels.Count;
        var headerRange = ws.Range(startRow, 1, startRow, colCount);
        headerRange.Style.Font.Bold = true;
        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

        // Data rows
        for (int i = 0; i < trends.Count; i++)
        {
            var row = startRow + 1 + i;
            var trend = trends[i];

            // Name from groupName or questionText
            var name = "";
            if (trend.TryGetProperty("groupName", out var gn)) name = gn.GetString() ?? "";
            else if (trend.TryGetProperty("questionText", out var qt)) name = qt.GetString() ?? "";

            ws.Cell(row, 1).Value = name;

            if (trend.TryGetProperty("scores", out var scoresEl) && scoresEl.ValueKind == JsonValueKind.Array)
            {
                var scores = scoresEl.EnumerateArray().ToList();
                for (int j = 0; j < scores.Count && j < monthLabels.Count; j++)
                {
                    if (scores[j].ValueKind == JsonValueKind.Number)
                        ws.Cell(row, 2 + j).Value = scores[j].GetDouble();
                    else if (scores[j].ValueKind == JsonValueKind.Null)
                        ws.Cell(row, 2 + j).Value = "";
                }
            }
        }
    }

    private static int GetJsonInt(JsonElement el, string property)
    {
        if (el.TryGetProperty(property, out var val) && val.ValueKind == JsonValueKind.Number)
            return val.GetInt32();
        return 0;
    }

    /// <summary>
    /// Müşterinin projeleri
    /// </summary>
    [HttpGet("projects")]
    public async Task<IActionResult> GetProjects([FromQuery] int? projectTypeId = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
        var result = await _cpDataService.GetProjectsAsync(customerId.Value, projectTypeId, allowedPersonnelIds);
        return Ok(result);
    }

    /// <summary>
    /// Son değerlendirmeler
    /// </summary>
    [HttpGet("evaluations/recent")]
    public async Task<IActionResult> GetRecentEvaluations([FromQuery] int count = 10)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.GetRecentEvaluationsAsync(customerId.Value, allowedPersonnelIds, count);
        return Ok(result);
    }

    /// <summary>
    /// Tüm değerlendirmeler (sayfalı + filtreli)
    /// </summary>
    [HttpGet("evaluations")]
    public async Task<IActionResult> GetEvaluations([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] int? projectId = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();
        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.GetEvaluationsAsync(customerId.Value, role, personnelId, allowedPersonnelIds, page, pageSize, projectId, startDate, endDate);
        return Ok(result);
    }

    /// <summary>
    /// Tüm değerlendirmeler Excel export
    /// </summary>
    [HttpGet("evaluations/export")]
    public async Task<IActionResult> ExportAllEvaluations([FromQuery] int? projectId = null, [FromQuery] DateTime? startDate = null, [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();
        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.ExportAllEvaluationsToExcelAsync(customerId.Value, role, personnelId, allowedPersonnelIds, projectId, startDate, endDate);
        return File(result.FileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.FileName);
    }

    private static string GetStatusText(int statusId)
    {
        return statusId switch
        {
            EvaluationStatuses.Ids.Pending => "Beklemede",
            EvaluationStatuses.Ids.Draft => "Taslak",
            EvaluationStatuses.Ids.InProgress => "Devam Ediyor",
            EvaluationStatuses.Ids.Completed => "Tamamlandı",
            EvaluationStatuses.Ids.Cancelled => "İptal Edildi",
            _ => EvaluationStatuses.GetById(statusId)?.SystemName ?? "Bilinmeyen"
        };
    }

    /// <summary>
    /// Proje performans raporu
    /// </summary>
    [HttpGet("reports/project-performance")]
    public async Task<IActionResult> GetProjectPerformance(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] bool? isInternal = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.GetProjectPerformanceAsync(customerId.Value, allowedPersonnelIds, startDate, endDate, projectIds, organizationIds, isInternal);
        return Ok(result);
    }

    /// <summary>
    /// Dönem özet raporu
    /// </summary>
    [HttpGet("reports/summary")]
    public async Task<IActionResult> GetReportSummary(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] bool? isInternal = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.GetReportSummaryAsync(customerId.Value, allowedPersonnelIds, startDate, endDate, projectIds, organizationIds, isInternal);
        return Ok(result);
    }

    /// <summary>
    /// Puan aralığına göre değerlendirmeler (detay modal için)
    /// </summary>
    [HttpGet("reports/score-range")]
    public async Task<IActionResult> GetEvaluationsByScoreRange(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] decimal minScore,
        [FromQuery] decimal maxScore,
        [FromQuery] bool? isInternal = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.GetEvaluationsByScoreRangeAsync(customerId.Value, allowedPersonnelIds, startDate, endDate, projectIds, organizationIds, minScore, maxScore, isInternal);
        return Ok(result);
    }

    /// <summary>
    /// Aylık trend raporu (tarih aralığına göre)
    /// </summary>
    [HttpGet("reports/monthly-trend")]
    public async Task<IActionResult> GetReportMonthlyTrend(
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] bool? isInternal = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

        var result = await _cpDataService.GetReportMonthlyTrendAsync(customerId.Value, allowedPersonnelIds, startDate, endDate, projectIds, organizationIds, isInternal);
        return Ok(result);
    }

    /// <summary>
    /// Organizasyonlar (gruplu - hiyerarşik)
    /// </summary>
    [HttpGet("organizations")]
    public async Task<IActionResult> GetOrganizations()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var allowedOrgIds = await GetAllowedOrganizationIdsAsync();
        var result = await _cpDataService.GetOrganizationsAsync(customerId.Value, allowedOrgIds);
        return Ok(result);
    }

    /// <summary>
    /// Organizasyonlar için gelişim trendi (tarih aralığına göre)
    /// Default: Bu haftanın başı (Pazartesi) - Bugün
    /// </summary>
    [HttpGet("organizations/monthly-trend")]
    public async Task<IActionResult> GetOrganizationsMonthlyTrend(
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

        var result = await _cpDataService.GetOrganizationsMonthlyTrendAsync(customerId.Value, allowedOrgIds, organizationIds, startDate, endDate);
        return Ok(result);
    }

    /// <summary>
    /// Süpervizörler (gruplu - organizasyona göre)
    /// Değerlendirme sayısı ve ortalaması: Süpervizörün takımındaki personelin ALDIĞI değerlendirmeler
    /// </summary>
    [HttpGet("supervisors")]
    public async Task<IActionResult> GetSupervisors(
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] string? searchText = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var allowedOrgIds = await GetAllowedOrganizationIdsAsync();
        var result = await _cpDataService.GetSupervisorsAsync(customerId.Value, allowedOrgIds, organizationIds, searchText);
        return Ok(result);
    }

    /// <summary>
    /// Süpervizör için aylık gelişim trendi (takımındaki personelin aldığı değerlendirmeler, son 12 ay)
    /// </summary>
    [HttpGet("supervisors/{supervisorId}/monthly-trend")]
    public async Task<IActionResult> GetSupervisorMonthlyTrend(int supervisorId)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFound") });

        var result = await _cpDataService.GetSupervisorMonthlyTrendAsync(customerId.Value, supervisorId);
        if (result == null)
            return NotFound(new { message = "Süpervizör bulunamadı" });

        return Ok(result);
    }

    /// <summary>
    /// İç dinlemeler (firma personeli tarafından yapılan)
    /// </summary>
    [HttpGet("evaluations/internal")]
    public async Task<IActionResult> GetInternalEvaluations(
        [FromQuery] int? page = 1,
        [FromQuery] int? pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] List<string>? evaluatorNames = null,
        [FromQuery] List<string>? personnelNames = null,
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] List<string>? callIds = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();

        var result = await _cpDataService.GetInternalEvaluationsAsync(customerId.Value, role, personnelId, page, pageSize, search, startDate, endDate, projectIds, evaluatorNames, personnelNames, organizationIds, callIds);
        return Ok(result);
    }

    #region Saved Filters

    /// <summary>
    /// CustomerPortal - Kayıtlı filtreleri getir (aynı customer'ın tüm personelleri görür)
    /// </summary>
    [HttpGet("saved-filters")]
    public async Task<IActionResult> GetSavedFilters([FromQuery] string page)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = "Müşteri bilgisi alınamadı" });

        var result = await _cpDataService.GetSavedFiltersAsync(customerId.Value, page);
        return Ok(result);
    }

    /// <summary>
    /// CustomerPortal - Filtre kaydet (CustomerId ile - tüm personeller görebilir)
    /// </summary>
    [HttpPost("saved-filters")]
    public async Task<IActionResult> SaveFilter([FromBody] CustomerSaveFilterRequest request)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = "Müşteri bilgisi alınamadı" });

        if (string.IsNullOrEmpty(request.Name))
            return BadRequest(new { message = "Filtre adı zorunludur" });

        var filterDataJson = System.Text.Json.JsonSerializer.Serialize(request.Filters);
        var id = await _cpDataService.SaveFilterAsync(customerId.Value, request.Name, request.Page, filterDataJson);
        return Ok(new { message = "Filtre kaydedildi", id });
    }

    /// <summary>
    /// CustomerPortal - Filtre sil (sadece kendi customer'ının filtresini silebilir)
    /// </summary>
    [HttpDelete("saved-filters/{id}")]
    public async Task<IActionResult> DeleteSavedFilter(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = "Müşteri bilgisi alınamadı" });

        var deleted = await _cpDataService.DeleteSavedFilterAsync(customerId.Value, id);
        if (!deleted)
            return NotFound(new { message = "Filtre bulunamadı" });

        return Ok(new { message = "Filtre silindi" });
    }

    public class CustomerSaveFilterRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Page { get; set; } = string.Empty;
        public List<object> Filters { get; set; } = new();
    }

    #endregion

    /// <summary>
    /// Dış dinlemeler (bizim tarafımızdan yapılan)
    /// </summary>
    [HttpGet("evaluations/external")]
    public async Task<IActionResult> GetExternalEvaluations(
        [FromQuery] int? page = 1,
        [FromQuery] int? pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] List<string>? personnelNames = null,
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] List<string>? callIds = null,
        [FromQuery] decimal? minScore = null,
        [FromQuery] decimal? maxScore = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        var role = GetPersonnelRole();
        var personnelId = GetPersonnelId();

        var result = await _cpDataService.GetExternalEvaluationsAsync(customerId.Value, role, personnelId, page, pageSize, search, startDate, endDate, projectIds, personnelNames, organizationIds, callIds, minScore, maxScore);
        return Ok(result);
    }

    /// <summary>
    /// Değerlendirme detayını getirir (CustomerPortal)
    /// </summary>
    [HttpGet("evaluations/{evaluationId}")]
    public async Task<IActionResult> GetEvaluationDetail(int evaluationId)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            var personnelId = GetPersonnelId();

            var accessCheck = await _cpDataService.CheckEvaluationAccessAsync(evaluationId, customerId.Value, allowedPersonnelIds, personnelId);
            if (accessCheck == null)
                return Forbid();
            if (!accessCheck.Value.IsAuthorized)
                return StatusCode(403, new { message = "Bu değerlendirmeyi görüntüleme yetkiniz bulunmamaktadır." });

            var detail = await _reportService.GetEvaluationDetailAsync(evaluationId);
            if (detail == null)
                return NotFound(new { message = "Değerlendirme bulunamadı." });

            return Ok(detail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error getting evaluation detail {EvaluationId} for customer {CustomerId}", evaluationId, customerId);
            return StatusCode(500, new { message = "Değerlendirme detayı yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Değerlendirme ekli dosyalarını getirir (CustomerPortal)
    /// </summary>
    [HttpGet("evaluations/{evaluationId}/attachments")]
    public async Task<IActionResult> GetEvaluationAttachments(int evaluationId)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            var personnelId = GetPersonnelId();
            var accessResult = await _cpDataService.CheckEvaluationAccessAsync(evaluationId, customerId.Value, allowedPersonnelIds, personnelId);

            if (accessResult == null)
                return NotFound(new { message = "Değerlendirme bulunamadı." });

            if (!accessResult.Value.IsAuthorized)
                return StatusCode(403, new { message = "Bu değerlendirmenin dosyalarını görüntüleme yetkiniz bulunmamaktadır." });

            var attachments = await _cpDataService.GetEvaluationAttachmentsAsync(evaluationId);
            return Ok(attachments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error getting evaluation attachments {EvaluationId} for customer {CustomerId}", evaluationId, customerId);
            return StatusCode(500, new { message = "Dosyalar yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Değerlendirme detayı Excel export (CustomerPortal)
    /// </summary>
    [HttpGet("evaluations/{evaluationId}/export")]
    public async Task<IActionResult> ExportEvaluationDetail(int evaluationId)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            var personnelId = GetPersonnelId();
            var accessResult = await _cpDataService.CheckEvaluationAccessAsync(evaluationId, customerId.Value, allowedPersonnelIds, personnelId);

            if (accessResult == null)
                return NotFound(new { message = "Değerlendirme bulunamadı." });

            if (!accessResult.Value.IsAuthorized)
                return StatusCode(403, new { message = "Bu değerlendirmeyi dışa aktarma yetkiniz bulunmamaktadır." });

            var result = await _reportService.ExportEvaluationDetailToExcelAsync(evaluationId, excludeEvaluatorInfo: true);
            if (result == null)
                return NotFound(new { message = "Değerlendirme bulunamadı." });

            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting evaluation detail {EvaluationId} for customer {CustomerId}", evaluationId, customerId);
            return StatusCode(500, new { message = "Değerlendirme export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Soru Grubu Ortalama Raporu - Excel export (CustomerPortal)
    /// </summary>
    [HttpPost("reports/export/question-group-average")]
    public async Task<IActionResult> ExportQuestionGroupAverageReport([FromBody] ReportFilterDto filter)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Müşteri sadece kendi projesinin raporunu görebilir
            filter.ProjectCustomerIds = new List<int> { customerId.Value };

            var result = await _reportService.ExportQuestionGroupAverageReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting question group average report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Rapor oluşturulurken hata oluştu." });
        }
    }

    /// <summary>
    /// Müşteri Değerlendirme Raporu - Excel export (CustomerPortal)
    /// </summary>
    [HttpPost("reports/export/customer-evaluation")]
    public async Task<IActionResult> ExportCustomerEvaluationReport([FromBody] ReportFilterDto filter)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Müşteri sadece kendi projesinin raporunu görebilir
            filter.ProjectCustomerIds = new List<int> { customerId.Value };

            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null)
                filter.PersonnelIds = allowedPersonnelIds;

            var result = await _reportService.ExportCustomerEvaluationReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting customer evaluation report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Rapor oluşturulurken hata oluştu." });
        }
    }

    /// <summary>
    /// İç Dinleme Raporu - Excel export (CustomerPortal - Dinleyen kolonu dahil)
    /// </summary>
    [HttpPost("reports/export/internal-evaluation")]
    public async Task<IActionResult> ExportInternalEvaluationReport([FromBody] ReportFilterDto filter)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Müşteri sadece kendi projesinin raporunu görebilir
            filter.ProjectCustomerIds = new List<int> { customerId.Value };

            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null)
                filter.PersonnelIds = allowedPersonnelIds;

            var result = await _reportService.ExportInternalEvaluationReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting internal evaluation report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Rapor oluşturulurken hata oluştu." });
        }
    }

    /// <summary>
    /// Puansız Soru Raporu - Excel export (CustomerPortal)
    /// </summary>
    [HttpPost("reports/export/unscored-questions")]
    public async Task<IActionResult> ExportUnscoredQuestionsReport([FromBody] ReportFilterDto filter)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Müşteri sadece kendi projesinin raporunu görebilir
            filter.ProjectCustomerIds = new List<int> { customerId.Value };

            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null)
                filter.PersonnelIds = allowedPersonnelIds;

            var result = await _reportService.ExportUnscoredQuestionsReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting unscored questions report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Rapor oluşturulurken hata oluştu." });
        }
    }

    /// <summary>
    /// Cezalı KL Raporu (CustomerPortal) - EvaluatorName hariç
    /// </summary>
    [HttpGet("reports/penalties")]
    public async Task<IActionResult> GetPenaltiesReport(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] List<string>? penaltyTypes,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

            // Supervisor için organizasyon kısıtlaması
            var effectiveOrgIds = organizationIds;
            if (allowedOrgIds != null)
            {
                if (organizationIds?.Any() == true)
                    effectiveOrgIds = organizationIds.Where(id => allowedOrgIds.Contains(id)).ToList();
                else
                    effectiveOrgIds = allowedOrgIds;
            }

            var filter = new PenaltyFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value }, // Otomatik müşteri filtresi
                OrganizationIds = effectiveOrgIds,
                ChecklistIds = checklistIds,
                PenaltyTypes = penaltyTypes,
                PersonnelIds = allowedPersonnelIds,
                Page = page,
                PageSize = pageSize
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.GetPenaltiesReportAsync(filter);

            // EvaluatorName alanlarını temizle (müşteri görmemeli)
            foreach (var penalty in result.Penalties)
            {
                penalty.EvaluatorName = null;
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading penalties report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Cezalı KL raporu yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Cezalı KL Raporu Excel Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/penalties/export")]
    public async Task<IActionResult> ExportPenaltiesToExcel(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] List<string>? penaltyTypes,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

            // Supervisor için organizasyon kısıtlaması
            var effectiveOrgIds = organizationIds;
            if (allowedOrgIds != null)
            {
                if (organizationIds?.Any() == true)
                    effectiveOrgIds = organizationIds.Where(id => allowedOrgIds.Contains(id)).ToList();
                else
                    effectiveOrgIds = allowedOrgIds;
            }

            var filter = new PenaltyFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value },
                OrganizationIds = effectiveOrgIds,
                ChecklistIds = checklistIds,
                PenaltyTypes = penaltyTypes,
                PersonnelIds = allowedPersonnelIds,
                Page = 1,
                PageSize = int.MaxValue
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.ExportPenaltiesToExcelAsync(filter, excludeEvaluator: true);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting penalties report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Cezalı KL raporu export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Öneriler Raporu (CustomerPortal) - EvaluatorName hariç
    /// </summary>
    [HttpGet("reports/suggestions")]
    public async Task<IActionResult> GetSuggestionsReport(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? organizationIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] string? searchText,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

            // Supervisor için organizasyon kısıtlaması
            var effectiveOrgIds = organizationIds;
            if (allowedOrgIds != null)
            {
                if (organizationIds?.Any() == true)
                    effectiveOrgIds = organizationIds.Where(id => allowedOrgIds.Contains(id)).ToList();
                else
                    effectiveOrgIds = allowedOrgIds;
            }

            var filter = new SuggestionsFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value }, // Otomatik müşteri filtresi
                OrganizationIds = effectiveOrgIds,
                ChecklistIds = checklistIds,
                PersonnelIds = allowedPersonnelIds,
                SearchText = searchText,
                Page = page,
                PageSize = pageSize
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.GetSuggestionsReportAsync(filter);

            // EvaluatorName alanlarını temizle (müşteri görmemeli)
            foreach (var suggestion in result.Suggestions)
            {
                suggestion.EvaluatorName = null;
            }

            // Değerlendirici sayısını gizle
            result.Summary.UniqueEvaluators = 0;

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading suggestions report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Öneriler raporu yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// En çok öneri yazılan sorular (CustomerPortal)
    /// </summary>
    [HttpGet("reports/suggestions/top-questions")]
    public async Task<IActionResult> GetTopSuggestedQuestions(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int top = 10)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

            var filter = new SuggestionsFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value },
                ChecklistIds = checklistIds,
                PersonnelIds = allowedPersonnelIds
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.GetTopSuggestedQuestionsAsync(filter, top);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading top suggested questions for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "En çok öneri yazılan sorular yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// En çok seçilen alt kriterler (CustomerPortal)
    /// </summary>
    [HttpGet("reports/suggestions/top-subcriteria")]
    public async Task<IActionResult> GetTopSubCriteria(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int top = 10)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

            var filter = new SuggestionsFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value },
                ChecklistIds = checklistIds,
                PersonnelIds = allowedPersonnelIds
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.GetTopSubCriteriaAsync(filter, top);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading top subcriteria for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "En çok seçilen alt kriterler yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// En çok seçilen alt kriterler Excel Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/suggestions/top-subcriteria/export")]
    public async Task<IActionResult> ExportTopSubCriteriaToExcel(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

            var filter = new SuggestionsFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value },
                ChecklistIds = checklistIds,
                PersonnelIds = allowedPersonnelIds
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var data = await _reportService.GetTopSubCriteriaAsync(filter, int.MaxValue);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("En Çok Seçilen Alt Kriterler");

            // Headers
            worksheet.Cell(1, 1).Value = "Alt Kriter";
            worksheet.Cell(1, 2).Value = "Soru";
            worksheet.Cell(1, 3).Value = "Checklist";
            worksheet.Cell(1, 4).Value = "Grup";
            worksheet.Cell(1, 5).Value = "Seçim Sayısı";
            worksheet.Cell(1, 6).Value = "Değerlendirme Sayısı";
            worksheet.Cell(1, 7).Value = "Toplam Değerlendirme";

            var headerRange = worksheet.Range(1, 1, 1, 7);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Data rows - already sorted by SelectionCount desc from service
            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.Description;
                worksheet.Cell(row, 2).Value = item.QuestionText;
                worksheet.Cell(row, 3).Value = item.ChecklistName;
                worksheet.Cell(row, 4).Value = item.GroupName;
                worksheet.Cell(row, 5).Value = item.SelectionCount;
                worksheet.Cell(row, 6).Value = item.EvaluationCount;
                worksheet.Cell(row, 7).Value = item.TotalQuestionEvaluations;
                row++;
            }

            worksheet.Columns().AdjustToContents();
            ExcelHelper.ApplyLongTextColumnStyles(worksheet);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"EnCokSecilenAltKriterler_{TurkeyTime.Now:yyyyMMdd}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting top subcriteria for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "En çok seçilen alt kriterler export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// En çok öneri yazılan sorular Excel Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/suggestions/top-questions/export")]
    public async Task<IActionResult> ExportTopSuggestedQuestionsToExcel(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate,
        [FromQuery] int top = 100)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

            var filter = new SuggestionsFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value },
                ChecklistIds = checklistIds,
                PersonnelIds = allowedPersonnelIds
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var data = await _reportService.GetTopSuggestedQuestionsAsync(filter, top);

            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("En Çok Önerilen Sorular");

            // Headers
            worksheet.Cell(1, 1).Value = "Soru";
            worksheet.Cell(1, 2).Value = "Checklist";
            worksheet.Cell(1, 3).Value = "Grup";
            worksheet.Cell(1, 4).Value = "Öneri Sayısı";
            worksheet.Cell(1, 5).Value = "Değerlendirme Sayısı";
            worksheet.Cell(1, 6).Value = "Ortalama Puan (%)";

            var headerRange = worksheet.Range(1, 1, 1, 6);
            headerRange.Style.Font.Bold = true;
            headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
            headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

            // Data rows
            int row = 2;
            foreach (var item in data)
            {
                worksheet.Cell(row, 1).Value = item.QuestionText;
                worksheet.Cell(row, 2).Value = item.ChecklistName;
                worksheet.Cell(row, 3).Value = item.GroupName;
                worksheet.Cell(row, 4).Value = item.SuggestionCount;
                worksheet.Cell(row, 5).Value = item.EvaluationCount;
                worksheet.Cell(row, 6).Value = Math.Round(item.AverageScore, 1);
                row++;
            }

            worksheet.Columns().AdjustToContents();
            ExcelHelper.ApplyLongTextColumnStyles(worksheet);

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            stream.Position = 0;

            var fileName = $"EnCokOnerilenSorular_{TurkeyTime.Now:yyyyMMdd}.xlsx";
            return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting top suggested questions for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "En çok önerilen sorular export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Öneriler Raporu Excel Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/suggestions/export")]
    public async Task<IActionResult> ExportSuggestionsToExcel(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<int>? checklistIds,
        [FromQuery] string? searchText,
        [FromQuery] DateTime? startDate,
        [FromQuery] DateTime? endDate)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();

            var filter = new SuggestionsFilterDto
            {
                ProjectIds = projectIds,
                CustomerIds = new List<int> { customerId.Value },
                ChecklistIds = checklistIds,
                PersonnelIds = allowedPersonnelIds,
                SearchText = searchText,
                Page = 1,
                PageSize = int.MaxValue
            };

            if (startDate.HasValue || endDate.HasValue)
            {
                filter.DateRanges = new List<DateRangeFilter>
                {
                    new DateRangeFilter { StartDate = startDate, EndDate = endDate }
                };
            }

            var result = await _reportService.ExportSuggestionsToExcelAsync(filter, excludeEvaluator: true);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting suggestions report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Öneriler raporu export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Temsilci Karnesi - Personel Listesi (CustomerPortal)
    /// </summary>
    [HttpGet("reports/personnel-list")]
    public async Task<IActionResult> GetPersonnelList([FromQuery] List<int>? organizationIds = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

            // Geriye uyumluluk: tekil organizationId parametresi için array'in ilk elemanını kullan
            var organizationId = organizationIds?.FirstOrDefault();
            var personnel = await _reportService.GetEvaluatedPersonnelListAsync(customerId.Value, organizationId);

            // Supervisor için sadece yetkili olduğu personeli filtrele
            if (allowedPersonnelIds != null)
            {
                personnel = personnel.Where(p => allowedPersonnelIds.Contains(p.Id));
            }

            // Supervisor için sadece yetkili olduğu organizasyonları filtrele
            if (allowedOrgIds != null)
            {
                personnel = personnel.Where(p => p.OrganizationId.HasValue && allowedOrgIds.Contains(p.OrganizationId.Value));
            }

            return Ok(personnel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading personnel list for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Personel listesi yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Temsilci Karnesi Raporu (CustomerPortal) - EvaluatorName hariç
    /// </summary>
    [HttpGet("reports/personnel-report-card/{personnelId}")]
    public async Task<IActionResult> GetPersonnelReportCard(
        int personnelId,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges,
        [FromQuery] string? evaluationType = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Güvenlik kontrolü: Personelin bu müşteriye ait olup olmadığını doğrula
            if (!await _cpDataService.ValidatePersonnelBelongsToCustomerAsync(personnelId, customerId.Value))
                return NotFound(new { message = "Temsilci bulunamadı." });

            // Supervisor erişim kontrolü
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null && !allowedPersonnelIds.Contains(personnelId))
                return StatusCode(403, new { message = "Bu temsilcinin karnesini görüntüleme yetkiniz bulunmamaktadır." });

            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds,
                DateRanges = dateRanges,
                EvaluationType = evaluationType
            };

            var result = await _reportService.GetPersonnelReportCardAsync(filter);

            if (result == null)
                return NotFound(new { message = "Temsilci bulunamadı." });

            // EvaluatorName alanlarını temizle (müşteri görmemeli)
            foreach (var evaluation in result.RecentEvaluations)
            {
                evaluation.EvaluatorName = null;
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading personnel report card for customer {CustomerId}, personnel {PersonnelId}", customerId, personnelId);
            return StatusCode(500, new { message = "Temsilci karnesi yüklenirken hata oluştu.", error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Kendi Karnem - CustomerOperator'ın kendi performans karnesini görüntüler
    /// Token'daki NameIdentifier (CustomerPersonnelId) ile kendi karnesini döner
    /// </summary>
    [HttpGet("reports/my-report-card")]
    public async Task<IActionResult> GetMyReportCard(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges,
        [FromQuery] string? evaluationType = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Token'dan CustomerPersonnelId'yi al
            var personnelIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;

            if (userType != "CustomerPersonnel" || string.IsNullOrEmpty(personnelIdClaim) || !int.TryParse(personnelIdClaim, out var personnelId))
            {
                return BadRequest(new { message = "Bu endpoint sadece müşteri personeli için kullanılabilir." });
            }

            // Personelin bu müşteriye ait olup olmadığını doğrula
            if (!await _cpDataService.ValidatePersonnelBelongsToCustomerAsync(personnelId, customerId.Value))
                return NotFound(new { message = "Personel bulunamadı." });

            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds,
                DateRanges = dateRanges,
                EvaluationType = evaluationType
            };

            var result = await _reportService.GetPersonnelReportCardAsync(filter);

            if (result == null)
                return NotFound(new { message = "Karne verisi bulunamadı." });

            // EvaluatorName alanlarını temizle (temsilci görmemeli)
            foreach (var evaluation in result.RecentEvaluations)
            {
                evaluation.EvaluatorName = null;
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading my report card for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Karne yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Kendi Karnem Excel Export - CustomerOperator'ın kendi karnesini Excel olarak indirir
    /// </summary>
    [HttpGet("reports/my-report-card/export")]
    public async Task<IActionResult> ExportMyReportCard(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges,
        [FromQuery] string? evaluationType = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Token'dan CustomerPersonnelId'yi al
            var personnelIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;

            if (userType != "CustomerPersonnel" || string.IsNullOrEmpty(personnelIdClaim) || !int.TryParse(personnelIdClaim, out var personnelId))
            {
                return BadRequest(new { message = "Bu endpoint sadece müşteri personeli için kullanılabilir." });
            }

            // Personelin bu müşteriye ait olup olmadığını doğrula
            if (!await _cpDataService.ValidatePersonnelBelongsToCustomerAsync(personnelId, customerId.Value))
                return NotFound(new { message = "Personel bulunamadı." });

            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds,
                DateRanges = dateRanges,
                EvaluationType = evaluationType
            };

            var result = await _reportService.ExportPersonnelReportCardToExcelAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting my report card for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Karne export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Kendi Karnem Word Export - CustomerOperator'ın kendi karnesini Word olarak indirir
    /// </summary>
    [HttpGet("reports/my-report-card/export-word")]
    public async Task<IActionResult> ExportMyReportCardToWord(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges,
        [FromQuery] string? evaluationType = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Token'dan CustomerPersonnelId'yi al
            var personnelIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;

            if (userType != "CustomerPersonnel" || string.IsNullOrEmpty(personnelIdClaim) || !int.TryParse(personnelIdClaim, out var personnelId))
            {
                return BadRequest(new { message = "Bu endpoint sadece müşteri personeli için kullanılabilir." });
            }

            // Personelin bu müşteriye ait olup olmadığını doğrula
            if (!await _cpDataService.ValidatePersonnelBelongsToCustomerAsync(personnelId, customerId.Value))
                return NotFound(new { message = "Personel bulunamadı." });

            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds,
                DateRanges = dateRanges,
                EvaluationType = evaluationType
            };

            var result = await _reportService.ExportPersonnelReportCardToWordAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting my report card to Word for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Karne Word olarak export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Temsilci Karnesi Excel Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/personnel-report-card/{personnelId}/export")]
    public async Task<IActionResult> ExportPersonnelReportCard(
        int personnelId,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges,
        [FromQuery] string? evaluationType = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Güvenlik kontrolü: Personelin bu müşteriye ait olup olmadığını doğrula
            if (!await _cpDataService.ValidatePersonnelBelongsToCustomerAsync(personnelId, customerId.Value))
                return NotFound(new { message = "Temsilci bulunamadı." });

            // Supervisor erişim kontrolü
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null && !allowedPersonnelIds.Contains(personnelId))
                return StatusCode(403, new { message = "Bu temsilcinin karnesini görüntüleme yetkiniz bulunmamaktadır." });

            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds,
                DateRanges = dateRanges,
                EvaluationType = evaluationType
            };

            var result = await _reportService.ExportPersonnelReportCardToExcelAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting personnel report card for customer {CustomerId}, personnel {PersonnelId}", customerId, personnelId);
            return StatusCode(500, new { message = "Temsilci karnesi export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Temsilci Karnesi Word Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/personnel-report-card/{personnelId}/export-word")]
    public async Task<IActionResult> ExportPersonnelReportCardToWord(
        int personnelId,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges,
        [FromQuery] string? evaluationType = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Personelin bu müşteriye ait olup olmadığını kontrol et
            if (!await _cpDataService.ValidatePersonnelBelongsToCustomerAsync(personnelId, customerId.Value))
                return NotFound(new { message = "Personel bulunamadı." });

            // Supervisor erişim kontrolü
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null && !allowedPersonnelIds.Contains(personnelId))
                return StatusCode(403, new { message = "Bu temsilcinin karnesini görüntüleme yetkiniz bulunmamaktadır." });

            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds,
                DateRanges = dateRanges,
                EvaluationType = evaluationType
            };

            var result = await _reportService.ExportPersonnelReportCardToWordAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting personnel report card to Word for customer {CustomerId}, personnel {PersonnelId}", customerId, personnelId);
            return StatusCode(500, new { message = "Temsilci karnesi Word export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Kendi Karnem PDF Export - CustomerOperator'ın kendi karnesini PDF olarak indirir
    /// </summary>
    [HttpGet("reports/my-report-card/export-pdf")]
    public async Task<IActionResult> ExportMyReportCardToPdf(
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges,
        [FromQuery] string? evaluationType = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var personnelIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userType = User.FindFirst("UserType")?.Value;

            if (userType != "CustomerPersonnel" || string.IsNullOrEmpty(personnelIdClaim) || !int.TryParse(personnelIdClaim, out var personnelId))
                return BadRequest(new { message = "Bu endpoint sadece müşteri personeli için kullanılabilir." });

            if (!await _cpDataService.ValidatePersonnelBelongsToCustomerAsync(personnelId, customerId.Value))
                return NotFound(new { message = "Personel bulunamadı." });

            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds,
                DateRanges = dateRanges,
                EvaluationType = evaluationType
            };

            var report = await _reportService.GetPersonnelReportCardAsync(filter);
            if (report == null)
                return NotFound(new { message = "Karne verisi bulunamadı." });

            // EvaluatorName alanlarını temizle
            foreach (var evaluation in report.RecentEvaluations)
                evaluation.EvaluatorName = null;

            var html = GenerateReportCardHtml(report);
            var pdfBytes = await _pdfService.GeneratePdfFromHtmlAsync(html);

            return File(pdfBytes, "application/pdf", $"Karneme_{TurkeyTime.Now:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting my report card to PDF for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Karne PDF olarak export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Temsilci Karnesi PDF Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/personnel-report-card/{personnelId}/export-pdf")]
    public async Task<IActionResult> ExportPersonnelReportCardToPdf(
        int personnelId,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges,
        [FromQuery] string? evaluationType = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var personnelName = await _cpDataService.GetPersonnelNameAsync(personnelId, customerId.Value);
            if (personnelName == null)
                return NotFound(new { message = "Personel bulunamadı." });

            // Supervisor erişim kontrolü
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            if (allowedPersonnelIds != null && !allowedPersonnelIds.Contains(personnelId))
                return StatusCode(403, new { message = "Bu temsilcinin karnesini görüntüleme yetkiniz bulunmamaktadır." });

            var filter = new PersonnelReportCardFilterDto
            {
                PersonnelId = personnelId,
                ProjectIds = projectIds,
                DateRanges = dateRanges,
                EvaluationType = evaluationType
            };

            var report = await _reportService.GetPersonnelReportCardAsync(filter);
            if (report == null)
                return NotFound(new { message = "Karne verisi bulunamadı." });

            var html = GenerateReportCardHtml(report);
            var pdfBytes = await _pdfService.GeneratePdfFromHtmlAsync(html);

            return File(pdfBytes, "application/pdf", $"TemsilciKarnesi_{personnelName.Value.FirstName}_{personnelName.Value.LastName}_{TurkeyTime.Now:yyyyMMdd}.pdf");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting personnel report card to PDF for customer {CustomerId}, personnel {PersonnelId}", customerId, personnelId);
            return StatusCode(500, new { message = "Temsilci karnesi PDF export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Temsilci Karnesi için HTML şablonu oluşturur
    /// </summary>
    private string GenerateReportCardHtml(PersonnelReportCardDto report)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'></head><body>");

        // Başlık
        sb.AppendLine($"<h1>Temsilci Karnesi</h1>");
        sb.AppendLine($"<h2>{report.PersonnelName}</h2>");
        if (!string.IsNullOrEmpty(report.Title))
            sb.AppendLine($"<p><strong>Ünvan:</strong> {report.Title}</p>");

        // Özet
        sb.AppendLine("<div class='card'><div class='card-header'>Performans Özeti</div>");
        sb.AppendLine("<table>");
        sb.AppendLine($"<tr><td>Toplam Değerlendirme</td><td><strong>{report.TotalEvaluations}</strong></td></tr>");
        sb.AppendLine($"<tr><td>Ortalama Puan</td><td><strong class='{GetScoreClass(report.AverageScore)}'>{report.AverageScore:F1}%</strong></td></tr>");
        sb.AppendLine($"<tr><td>En Yüksek Puan</td><td class='text-success'>{report.BestScore:F1}%</td></tr>");
        sb.AppendLine($"<tr><td>En Düşük Puan</td><td class='text-danger'>{report.WorstScore:F1}%</td></tr>");
        sb.AppendLine($"<tr><td>Sarı Kart</td><td>{report.TotalYellowCards}</td></tr>");
        sb.AppendLine($"<tr><td>Kırmızı Kart</td><td>{report.TotalRedCards}</td></tr>");
        sb.AppendLine("</table></div>");

        // Aylık Trend
        if (report.MonthlyTrend.Any())
        {
            sb.AppendLine("<div class='card'><div class='card-header'>Aylık Performans</div>");
            sb.AppendLine("<table><thead><tr><th>Dönem</th><th class='text-center'>Değerlendirme</th><th class='text-center'>Ort. Puan</th><th class='text-center'>S.Kart</th><th class='text-center'>K.Kart</th></tr></thead><tbody>");
            foreach (var trend in report.MonthlyTrend)
            {
                sb.AppendLine($"<tr><td>{trend.MonthName}</td><td class='text-center'>{trend.EvaluationCount}</td><td class='text-center'><span class='badge {GetBadgeClass(trend.AverageScore)}'>{trend.AverageScore:F1}%</span></td><td class='text-center'>{trend.YellowCards}</td><td class='text-center'>{trend.RedCards}</td></tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        // Grup Performansı
        if (report.GroupPerformances.Any())
        {
            sb.AppendLine("<div class='card'><div class='card-header'>Grup Performansı</div>");
            sb.AppendLine("<table><thead><tr><th>Grup</th><th class='text-center'>Başarı</th></tr></thead><tbody>");
            foreach (var group in report.GroupPerformances)
            {
                sb.AppendLine($"<tr><td>{group.GroupName}</td><td class='text-center'><span class='badge {GetBadgeClass(group.PercentageScore)}'>{group.PercentageScore:F1}%</span></td></tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        // Güçlü/Zayıf Yönler
        if (report.Strengths.Any() || report.Weaknesses.Any())
        {
            sb.AppendLine("<div class='card'><div class='card-header'>Güçlü ve Zayıf Yönler</div>");

            if (report.Strengths.Any())
            {
                sb.AppendLine("<h3 class='text-success'>Güçlü Yönler</h3><ul>");
                foreach (var s in report.Strengths.Take(5))
                    sb.AppendLine($"<li><span class='badge bg-success'>{s.PercentageScore:F0}%</span> {s.QuestionText}</li>");
                sb.AppendLine("</ul>");
            }

            if (report.Weaknesses.Any())
            {
                sb.AppendLine("<h3 class='text-danger'>Geliştirilmeli</h3><ul>");
                foreach (var w in report.Weaknesses.Take(5))
                    sb.AppendLine($"<li><span class='badge bg-danger'>{w.PercentageScore:F0}%</span> {w.QuestionText}</li>");
                sb.AppendLine("</ul>");
            }

            sb.AppendLine("</div>");
        }

        // Son Değerlendirmeler
        if (report.RecentEvaluations.Any())
        {
            sb.AppendLine("<div class='page-break'></div>");
            sb.AppendLine("<div class='card'><div class='card-header'>Son Değerlendirmeler</div>");
            sb.AppendLine("<table><thead><tr><th>Tarih</th><th>Proje</th><th>Kontrol Listesi</th><th class='text-center'>Puan</th><th class='text-center'>Kartlar</th></tr></thead><tbody>");
            foreach (var eval in report.RecentEvaluations.Take(20))
            {
                var cards = "";
                if (eval.YellowCards > 0) cards += $"<span class='badge bg-warning'>{eval.YellowCards}</span> ";
                if (eval.RedCards > 0) cards += $"<span class='badge bg-danger'>{eval.RedCards}</span>";
                if (string.IsNullOrEmpty(cards)) cards = "-";

                sb.AppendLine($"<tr><td>{eval.EvaluationDate?.ToString("dd.MM.yyyy") ?? "-"}</td><td>{eval.ProjectName}</td><td>{eval.ChecklistName}</td><td class='text-center'><span class='badge {GetBadgeClass(eval.ScorePercentage)}'>{eval.ScorePercentage:F1}%</span></td><td class='text-center'>{cards}</td></tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        sb.AppendLine($"<p style='text-align:right;font-size:9pt;color:#999;margin-top:20px;'>Oluşturulma: {TurkeyTime.Now:dd.MM.yyyy HH:mm}</p>");
        sb.AppendLine("</body></html>");

        return sb.ToString();
    }

    private string GetScoreClass(decimal score)
    {
        if (score >= 90) return "text-success";
        if (score >= 60) return "text-warning";
        return "text-danger";
    }

    private string GetBadgeClass(decimal score)
    {
        if (score >= 90) return "bg-success";
        if (score >= 60) return "bg-warning";
        return "bg-danger";
    }

    /// <summary>
    /// Proje Performans Raporu - Excel export (CustomerPortal)
    /// </summary>
    [HttpPost("reports/export/project-performance")]
    public async Task<IActionResult> ExportProjectPerformanceReport([FromBody] ReportFilterDto filter)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Müşteri sadece kendi projesinin raporunu görebilir
            filter.ProjectCustomerIds = new List<int> { customerId.Value };

            var result = await _reportService.ExportProjectPerformanceReportAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting project performance report for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Rapor oluşturulurken hata oluştu." });
        }
    }

    /// <summary>
    /// Dönemlere Göre Personel Başarı Tablosu (CustomerPortal)
    /// </summary>
    [HttpGet("reports/performance-by-period")]
    public async Task<IActionResult> GetPerformanceByPeriod(
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

            var result = await _cpDataService.GetPerformanceByPeriodAsync(customerId.Value, allowedPersonnelIds, allowedOrgIds, projectIds, organizationIds, startDate, endDate);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading performance by period for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Rapor yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Dönemlere Göre Personel Başarı Tablosu - Excel Export (CustomerPortal)
    /// </summary>
    [HttpGet("reports/performance-by-period/export")]
    public async Task<IActionResult> ExportPerformanceByPeriod(
        [FromQuery] List<int>? projectIds = null,
        [FromQuery] List<int>? organizationIds = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Supervisor filtrelemesi
            var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
            var allowedOrgIds = await GetAllowedOrganizationIdsAsync();

            var result = await _cpDataService.ExportPerformanceByPeriodAsync(customerId.Value, allowedPersonnelIds, allowedOrgIds, projectIds, organizationIds, startDate, endDate);
            if (result == null)
                return NotFound(new { message = "Veri bulunamadı." });

            return File(result.Value.FileContent, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", result.Value.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting performance by period for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Excel oluşturulurken hata oluştu." });
        }
    }

    // ==================== EĞİTİM VİDEOLARI ====================

    /// <summary>
    /// Kullanıcının kendi eğitim videolarını listele
    /// </summary>
    [HttpGet("my-trainings")]
    public async Task<IActionResult> GetMyTrainings()
    {
        var personnelId = GetPersonnelId();

        // Admin kullanıcılar için boş liste dön (PersonnelId yok)
        if (!personnelId.HasValue)
            return Ok(new List<object>());

        var result = await _cpDataService.GetMyTrainingsAsync(personnelId.Value);
        return Ok(result);
    }

    /// <summary>
    /// Eğitim izleme ilerleme güncelle
    /// </summary>
    [HttpPost("my-trainings/{participantId}/progress")]
    public async Task<IActionResult> UpdateMyTrainingProgress(int participantId, [FromBody] UpdateWatchProgressDto dto)
    {
        var personnelId = GetPersonnelId();
        if (!personnelId.HasValue)
            return Unauthorized(new { message = "Personel bilgisi bulunamadı." });

            var result = await _cpDataService.UpdateMyTrainingProgressAsync(participantId, personnelId.Value, dto.WatchedSeconds, dto.IsCompleted);
            if (result == null)
                return NotFound(new { message = "Eğitim kaydı bulunamadı." });

            return Ok(result);
    }

    /// <summary>
    /// Video izleme oturumu başlat - izleme hakkını kullanır
    /// </summary>
    [HttpPost("my-trainings/{participantId}/start-session")]
    public async Task<IActionResult> StartWatchSession(int participantId)
    {
        var personnelId = GetPersonnelId();
        if (!personnelId.HasValue)
            return Unauthorized(new { message = "Personel bilgisi bulunamadı." });

            var result = await _cpDataService.StartWatchSessionAsync(participantId, personnelId.Value);
            if (result == null)
                return NotFound(new { message = "Eğitim kaydı bulunamadı." });

            // Check if max watches exceeded (service returns success=false in that case)
            var resultType = result.GetType();
            var successProp = resultType.GetProperty("success");
            if (successProp != null && (bool)successProp.GetValue(result)! == false)
                return BadRequest(result);

            return Ok(result);
    }

    /// <summary>
    /// Yönetici/Süpervizör için personel eğitimlerini listele
    /// </summary>
    [HttpGet("staff-trainings")]
    public async Task<IActionResult> GetStaffTrainings()
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var allowedPersonnelIds = await GetAllowedPersonnelIdsAsync();
        var result = await _cpDataService.GetStaffTrainingsAsync(customerId.Value, allowedPersonnelIds);
        return Ok(result);
    }

    /// <summary>
    /// İzleme ilerleme DTO
    /// </summary>
    public class UpdateWatchProgressDto
    {
        public int WatchedSeconds { get; set; }
        public bool IsCompleted { get; set; }
    }

    // ==================== SURVEY RESULTS (Reports) ====================

    /// <summary>
    /// Müşterinin Online Survey projelerini listeler
    /// </summary>
    [HttpGet("reports/survey-projects")]
    public async Task<IActionResult> GetSurveyProjects()
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var result = await _cpReportService.GetSurveyProjectsAsync(customerId.Value);
        return Ok(result);
    }

    /// <summary>
    /// Son anket yanıtlarını listeler
    /// </summary>
    [HttpGet("reports/survey-responses/recent")]
    public async Task<IActionResult> GetRecentSurveyResponses(
        [FromQuery] int count = 20,
        [FromQuery] int? projectId = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var result = await _cpReportService.GetRecentSurveyResponsesAsync(customerId.Value, count, projectId, startDate, endDate);
        return Ok(result);
    }

    /// <summary>
    /// Proje detayı - grup puanları ve son katılımcılar
    /// </summary>
    [HttpGet("reports/survey-projects/{projectId}/detail")]
    public async Task<IActionResult> GetSurveyProjectDetail(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var result = await _cpReportService.GetSurveyProjectDetailAsync(customerId.Value, projectId);
        if (result == null)
            return NotFound(new { message = "Proje bulunamadı." });

        return Ok(result);
    }

    /// <summary>
    /// Soru bazlı puan dağılımı
    /// </summary>
    [HttpGet("reports/survey-question-distribution")]
    public async Task<IActionResult> GetSurveyQuestionScoreDistribution(
        [FromQuery] int? projectId = null)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var result = await _cpReportService.GetSurveyQuestionScoreDistributionAsync(customerId.Value, projectId);
        return Ok(result);
    }

    /// <summary>
    /// Proje için soru bazlı puan detayı
    /// </summary>
    [HttpGet("reports/survey-projects/{projectId}/score-detail")]
    public async Task<IActionResult> GetSurveyQuestionScoreDetail(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var result = await _cpReportService.GetSurveyQuestionScoreDetailAsync(customerId.Value, projectId);
        if (result == null)
            return NotFound(new { message = "Proje bulunamadı." });

        return Ok(result);
    }

    /// <summary>
    /// Grup puanları raporu Excel export
    /// </summary>
    [HttpGet("reports/survey-results/{projectId}/export/group-scores")]
    public async Task<IActionResult> ExportSurveyGroupScores(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        // Proje kontrolü
        if (!await _cpDataService.ValidateSurveyProjectBelongsToCustomerAsync(projectId, customerId.Value))
            return NotFound(new { message = "Proje bulunamadı." });

        var result = await _reportService.ExportSurveyGroupScoresToExcelAsync(projectId);
        if (result == null)
            return NotFound(new { message = "Rapor oluşturulamadı." });

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Soru istatistikleri raporu Excel export
    /// </summary>
    [HttpGet("reports/survey-results/{projectId}/export/question-stats")]
    public async Task<IActionResult> ExportSurveyQuestionStats(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        if (!await _cpDataService.ValidateSurveyProjectBelongsToCustomerAsync(projectId, customerId.Value))
            return NotFound(new { message = "Proje bulunamadı." });

        var result = await _reportService.ExportSurveyQuestionStatsToExcelAsync(projectId);
        if (result == null)
            return NotFound(new { message = "Rapor oluşturulamadı." });

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Detay raporu Excel export (yorumsuz)
    /// </summary>
    [HttpGet("reports/survey-results/{projectId}/export/detail")]
    public async Task<IActionResult> ExportSurveyDetailReport(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        if (!await _cpDataService.ValidateSurveyProjectBelongsToCustomerAsync(projectId, customerId.Value))
            return NotFound(new { message = "Proje bulunamadı." });

        var result = await _reportService.ExportSurveyDetailReportToExcelAsync(projectId, includeComments: false);
        if (result == null)
            return NotFound(new { message = "Rapor oluşturulamadı." });

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Detay raporu Excel export (yorumlu)
    /// </summary>
    [HttpGet("reports/survey-results/{projectId}/export/full-detail")]
    public async Task<IActionResult> ExportSurveyFullDetailReport(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        if (!await _cpDataService.ValidateSurveyProjectBelongsToCustomerAsync(projectId, customerId.Value))
            return NotFound(new { message = "Proje bulunamadı." });

        var result = await _reportService.ExportSurveyDetailReportToExcelAsync(projectId, includeComments: true);
        if (result == null)
            return NotFound(new { message = "Rapor oluşturulamadı." });

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Soru puan detay raporu Excel export
    /// </summary>
    [HttpGet("reports/survey-results/{projectId}/export/score-detail")]
    public async Task<IActionResult> ExportSurveyScoreDetail(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        if (!await _cpDataService.ValidateSurveyProjectBelongsToCustomerAsync(projectId, customerId.Value))
            return NotFound(new { message = "Proje bulunamadı." });

        var result = await _reportService.ExportSurveyQuestionScoreDetailAsync(projectId);
        if (result == null)
            return NotFound(new { message = "Rapor oluşturulamadı." });

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Taslak değerlendirmeyi siler
    /// - Kullanıcı kendi taslağını silebilir
    /// - CustomerManager tüm taslakları silebilir
    /// - Sadece Draft durumundakiler silinebilir
    /// </summary>
    [HttpDelete("evaluations/{id:int}")]
    public async Task<IActionResult> DeleteDraft(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

            var role = GetPersonnelRole();
            var personnelId = GetPersonnelId();
            var isManager = role == "CustomerManager" || User.IsInRole("Admin");

            var result = await _cpDataService.DeleteDraftAsync(id, customerId.Value, isManager ? "CustomerManager" : role, personnelId);

            if (result == null)
                return NotFound(new { message = "Değerlendirme bulunamadı." });

            if (!result.Value.Success)
            {
                if (result.Value.StatusCode == 403) return Forbid();
                if (result.Value.StatusCode == 404) return NotFound(new { message = result.Value.ErrorMessage });
                return BadRequest(new { message = result.Value.ErrorMessage });
            }

            _logger.LogInformation("Taslak değerlendirme silindi (CustomerPortal): EvaluationId={EvaluationId}, PersonnelId={PersonnelId}, IsManager={IsManager}",
                id, personnelId, isManager);

            return Ok(new { message = "Taslak başarıyla silindi." });
    }

    /// <summary>
    /// İç dinleme taslağını siler (Sadece Manager)
    /// </summary>
    [HttpDelete("evaluations/internal/{id:int}")]
    public async Task<IActionResult> DeleteInternalDraft(int id)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

            var role = GetPersonnelRole();
            var isManager = role == "CustomerManager" || User.IsInRole("Admin");

            // Sadece Manager silebilir
            if (!isManager)
            {
                return Forbid();
            }

            var result = await _cpDataService.DeleteInternalDraftAsync(id, customerId.Value);

            if (result == null)
                return NotFound(new { message = "Değerlendirme bulunamadı." });

            if (!result.Value.Success)
            {
                if (result.Value.StatusCode == 403) return Forbid();
                if (result.Value.StatusCode == 404) return NotFound(new { message = result.Value.ErrorMessage });
                return BadRequest(new { message = result.Value.ErrorMessage });
            }

            _logger.LogInformation("İç dinleme taslağı silindi (CustomerPortal): EvaluationId={EvaluationId}, Role={Role}",
                id, role);

            return Ok(new { message = "Taslak başarıyla silindi." });
    }

    // ==================== ENNEAGRAM RESULTS ====================

    /// <summary>
    /// Müşterinin Enneagram projelerini listeler
    /// </summary>
    [HttpGet("reports/enneagram-projects")]
    public async Task<IActionResult> GetEnneagramProjects()
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var result = await _cpReportService.GetEnneagramProjectsAsync(customerId.Value);
        return Ok(result);
    }

    /// <summary>
    /// Enneagram sonuçlarını listeler
    /// </summary>
    [HttpGet("reports/enneagram-results")]
    public async Task<IActionResult> GetEnneagramResults(
        [FromQuery] int? projectId = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var result = await _cpReportService.GetEnneagramResultsAsync(customerId.Value, projectId, searchTerm, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Tek bir Enneagram sonuç detayı
    /// </summary>
    [HttpGet("reports/enneagram-results/{evaluationId}")]
    public async Task<IActionResult> GetEnneagramResultDetail(int evaluationId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var result = await _cpReportService.GetEnneagramResultDetailAsync(customerId.Value, evaluationId);
        if (result == null)
            return NotFound(new { message = "Sonuç bulunamadı." });

        return Ok(result);
    }

    /// <summary>
    /// Enneagram kişilik tipi dağılımı (proje bazlı)
    /// </summary>
    [HttpGet("reports/enneagram-distribution/{projectId}")]
    public async Task<IActionResult> GetEnneagramDistribution(int projectId)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var result = await _cpReportService.GetEnneagramDistributionAsync(customerId.Value, projectId);
        if (result == null)
            return NotFound(new { message = "Proje bulunamadı." });

        return Ok(result);
    }

    // ==================== SURVEY & ENNEAGRAM EXPORT ENDPOINTS ====================

    /// <summary>
    /// Anket yanıtlarını Excel'e export eder (proje bazlı)
    /// </summary>
    [HttpGet("reports/survey-responses/export")]
    public async Task<IActionResult> ExportSurveyResponses([FromQuery] int? projectId = null)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        if (!projectId.HasValue)
            return BadRequest(new { message = "Proje seçimi zorunludur." });

        if (!await _cpDataService.ValidateSurveyProjectBelongsToCustomerAsync(projectId.Value, customerId.Value))
            return NotFound(new { message = "Proje bulunamadı." });

        var result = await _reportService.ExportSurveyResponsesToExcelAsync(projectId);
        if (result == null)
            return NotFound(new { message = "Rapor oluşturulamadı." });

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Soru puan dağılımı Excel export
    /// </summary>
    [HttpGet("reports/survey-question-distribution/export")]
    public async Task<IActionResult> ExportSurveyQuestionDistribution([FromQuery] int? projectId = null)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        if (!projectId.HasValue)
            return BadRequest(new { message = "Proje seçimi zorunludur." });

        if (!await _cpDataService.ValidateSurveyProjectBelongsToCustomerAsync(projectId.Value, customerId.Value))
            return NotFound(new { message = "Proje bulunamadı." });

        var result = await _reportService.ExportSurveyQuestionDistributionToExcelAsync(projectId.Value);
        if (result == null)
            return NotFound(new { message = "Rapor oluşturulamadı." });

        return File(result.FileContent, result.ContentType, result.FileName);
    }

    /// <summary>
    /// Enneagram sonuçlarını Excel'e export eder
    /// </summary>
    [HttpGet("reports/enneagram-results/export")]
    public async Task<IActionResult> ExportEnneagramResults(
        [FromQuery] int? projectId = null,
        [FromQuery] string? searchTerm = null)
    {
        var customerId = GetCustomerId();
        if (!customerId.HasValue)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

            var projectIdsResult = await _cpDataService.GetEnneagramProjectIdsForCustomerAsync(customerId.Value, projectId);
            if (projectIdsResult == null)
                return NotFound(new { message = "Proje bulunamadı." });

            List<int> projectIds = projectIdsResult;

        var filter = new Core.DTOs.Report.EnneagramFilterDto
        {
            ProjectIds = projectIds,
            SearchTerm = searchTerm
        };

        var result = await _reportService.ExportEnneagramResultsToExcelAsync(filter);
        return File(result.FileContent, result.ContentType, result.FileName);
    }

    // ==================== PUAN EŞİKLERİ (Score Thresholds) ====================

    /// <summary>
    /// Müşteriye özel puan eşiklerini getirir (tüm proje tipleri için)
    /// </summary>
    [HttpGet("score-thresholds")]
    public async Task<IActionResult> GetScoreThresholds()
    {
        try
        {
            var customerId = GetCustomerId();
            if (!customerId.HasValue)
                return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

            var result = await _customerScoreThresholdService.GetAllAsync(customerId.Value);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error getting score thresholds");
            return StatusCode(500, new { message = "Puan eşikleri yüklenirken hata oluştu." });
        }
    }

    /// <summary>
    /// Müşteriye özel puan eşiklerini toplu kaydeder
    /// </summary>
    [HttpPost("score-thresholds/bulk")]
    public async Task<IActionResult> BulkSaveScoreThresholds([FromBody] Core.DTOs.CustomerScoreThreshold.BulkSaveCustomerScoreThresholdDto dto)
    {
        try
        {
            var customerId = GetCustomerId();
            if (!customerId.HasValue)
                return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var result = await _customerScoreThresholdService.BulkSaveAsync(customerId.Value, dto, userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error saving score thresholds");
            return StatusCode(500, new { message = "Puan eşikleri kaydedilirken hata oluştu." });
        }
    }

    // ===== ŞUBE KARNESİ =====

    [HttpGet("reports/dealer-list")]
    public async Task<IActionResult> GetDealerList()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            var dealers = await _reportService.GetDealerListAsync(customerId.Value);
            return Ok(dealers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading dealer list for customer {CustomerId}", customerId);
            return StatusCode(500, new { message = "Şube listesi yüklenirken hata oluştu." });
        }
    }

    [HttpGet("reports/dealer-report-card/{dealerId}")]
    public async Task<IActionResult> GetDealerReportCard(
        int dealerId,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            // Güvenlik kontrolü: Dealer'ın bu müşteriye ait olup olmadığını doğrula
            if (!await _cpDataService.ValidateDealerBelongsToCustomerAsync(dealerId, customerId.Value))
                return NotFound(new { message = "Şube bulunamadı." });

            var filter = new DealerReportCardFilterDto
            {
                DealerId = dealerId,
                ProjectIds = projectIds,
                DateRanges = dateRanges
            };

            var result = await _reportService.GetDealerReportCardAsync(filter);

            if (result == null)
                return NotFound(new { message = "Şube bulunamadı." });

            // EvaluatorName alanlarını temizle (müşteri görmemeli)
            foreach (var evaluation in result.RecentEvaluations)
            {
                evaluation.EvaluatorName = null;
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error loading dealer report card for customer {CustomerId}, dealer {DealerId}", customerId, dealerId);
            return StatusCode(500, new { message = "Şube karnesi yüklenirken hata oluştu.", error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    [HttpGet("reports/dealer-report-card/{dealerId}/export")]
    public async Task<IActionResult> ExportDealerReportCard(
        int dealerId,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            if (!await _cpDataService.ValidateDealerBelongsToCustomerAsync(dealerId, customerId.Value))
                return NotFound(new { message = "Şube bulunamadı." });

            var filter = new DealerReportCardFilterDto
            {
                DealerId = dealerId,
                ProjectIds = projectIds,
                DateRanges = dateRanges
            };

            var result = await _reportService.ExportDealerReportCardToExcelAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting dealer report card for customer {CustomerId}, dealer {DealerId}", customerId, dealerId);
            return StatusCode(500, new { message = "Şube karnesi export edilirken hata oluştu." });
        }
    }

    [HttpGet("reports/dealer-report-card/{dealerId}/export-word")]
    public async Task<IActionResult> ExportDealerReportCardToWord(
        int dealerId,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            if (!await _cpDataService.ValidateDealerBelongsToCustomerAsync(dealerId, customerId.Value))
                return NotFound(new { message = "Şube bulunamadı." });

            var filter = new DealerReportCardFilterDto
            {
                DealerId = dealerId,
                ProjectIds = projectIds,
                DateRanges = dateRanges
            };

            var result = await _reportService.ExportDealerReportCardToWordAsync(filter);
            return File(result.FileContent, result.ContentType, result.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting dealer report card to Word for customer {CustomerId}, dealer {DealerId}", customerId, dealerId);
            return StatusCode(500, new { message = "Şube karnesi Word export edilirken hata oluştu." });
        }
    }

    /// <summary>
    /// Şube Karnesi PDF export (CustomerPortal - PdfService kullanarak)
    /// </summary>
    [HttpGet("reports/dealer-report-card/{dealerId}/export-pdf")]
    public async Task<IActionResult> ExportDealerReportCardToPdf(
        int dealerId,
        [FromQuery] List<int>? projectIds,
        [FromQuery] List<DateRangeFilter>? dateRanges)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortal.CustomerNotFoundTokenInvalid") });

        try
        {
            if (!await _cpDataService.ValidateDealerBelongsToCustomerAsync(dealerId, customerId.Value))
                return NotFound(new { message = "Şube bulunamadı." });

            var filter = new DealerReportCardFilterDto
            {
                DealerId = dealerId,
                ProjectIds = projectIds,
                DateRanges = dateRanges
            };

            var report = await _reportService.GetDealerReportCardAsync(filter);
            if (report == null)
                return NotFound(new { message = "Şube karnesi verisi bulunamadı." });

            var html = GenerateDealerReportCardHtml(report);
            var pdfBytes = await _pdfService.GeneratePdfFromHtmlAsync(html);

            var fileName = $"SubeKarnesi_{report.DealerName.Replace(" ", "_")}_{TurkeyTime.Now:yyyyMMdd}.pdf";
            return File(pdfBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error exporting dealer report card PDF for customer {CustomerId}, dealer {DealerId}", customerId, dealerId);
            return StatusCode(500, new { message = "Şube karnesi PDF oluşturulurken hata oluştu." });
        }
    }

    private string GenerateDealerReportCardHtml(DealerReportCardDto report)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: 'Segoe UI', Arial, sans-serif; font-size: 11pt; color: #333; margin: 20px; }");
        sb.AppendLine("h1 { color: #0d6efd; border-bottom: 2px solid #0d6efd; padding-bottom: 8px; }");
        sb.AppendLine("h2 { color: #495057; margin-top: 5px; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-bottom: 15px; }");
        sb.AppendLine("th, td { border: 1px solid #dee2e6; padding: 6px 10px; text-align: left; }");
        sb.AppendLine("th { background-color: #f8f9fa; font-weight: 600; }");
        sb.AppendLine(".text-center { text-align: center; }");
        sb.AppendLine(".text-success { color: #198754; } .text-warning { color: #ffc107; } .text-danger { color: #dc3545; }");
        sb.AppendLine(".badge { display: inline-block; padding: 3px 8px; border-radius: 4px; color: #fff; font-size: 10pt; }");
        sb.AppendLine(".bg-success { background-color: #198754; } .bg-warning { background-color: #ffc107; color: #000; } .bg-danger { background-color: #dc3545; }");
        sb.AppendLine(".card { border: 1px solid #dee2e6; border-radius: 6px; margin-bottom: 15px; }");
        sb.AppendLine(".card-header { background-color: #f8f9fa; padding: 8px 12px; font-weight: 600; border-bottom: 1px solid #dee2e6; }");
        sb.AppendLine(".page-break { page-break-before: always; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine($"<h1>Şube Karnesi</h1>");
        sb.AppendLine($"<h2>{report.DealerName}</h2>");
        if (!string.IsNullOrEmpty(report.DealerCode))
            sb.AppendLine($"<p><strong>Şube Kodu:</strong> {report.DealerCode}</p>");
        if (!string.IsNullOrEmpty(report.City))
            sb.AppendLine($"<p><strong>Konum:</strong> {report.City}{(string.IsNullOrEmpty(report.District) ? "" : " / " + report.District)}</p>");

        sb.AppendLine("<div class='card'><div class='card-header'>Performans Özeti</div>");
        sb.AppendLine("<table>");
        sb.AppendLine($"<tr><td>Toplam Değerlendirme</td><td><strong>{report.TotalEvaluations}</strong></td></tr>");
        sb.AppendLine($"<tr><td>Ortalama Puan</td><td><strong class='{(report.AverageScore >= 80 ? "text-success" : report.AverageScore >= 60 ? "text-warning" : "text-danger")}'>{report.AverageScore:F1}%</strong></td></tr>");
        sb.AppendLine($"<tr><td>En Yüksek Puan</td><td class='text-success'>{report.BestScore:F1}%</td></tr>");
        sb.AppendLine($"<tr><td>En Düşük Puan</td><td class='text-danger'>{report.WorstScore:F1}%</td></tr>");
        sb.AppendLine($"<tr><td>Sarı Kart</td><td>{report.TotalYellowCards}</td></tr>");
        sb.AppendLine($"<tr><td>Kırmızı Kart</td><td>{report.TotalRedCards}</td></tr>");
        sb.AppendLine("</table></div>");

        if (report.MonthlyTrend.Any())
        {
            sb.AppendLine("<div class='card'><div class='card-header'>Aylık Performans</div>");
            sb.AppendLine("<table><thead><tr><th>Dönem</th><th class='text-center'>Değerlendirme</th><th class='text-center'>Ort. Puan</th><th class='text-center'>S.Kart</th><th class='text-center'>K.Kart</th></tr></thead><tbody>");
            foreach (var t in report.MonthlyTrend)
                sb.AppendLine($"<tr><td>{t.MonthName}</td><td class='text-center'>{t.EvaluationCount}</td><td class='text-center'><span class='badge {(t.AverageScore >= 80 ? "bg-success" : t.AverageScore >= 60 ? "bg-warning" : "bg-danger")}'>{t.AverageScore:F1}%</span></td><td class='text-center'>{t.YellowCards}</td><td class='text-center'>{t.RedCards}</td></tr>");
            sb.AppendLine("</tbody></table></div>");
        }

        if (report.GroupPerformances.Any())
        {
            sb.AppendLine("<div class='card'><div class='card-header'>Grup Performansı</div>");
            sb.AppendLine("<table><thead><tr><th>Grup</th><th class='text-center'>Başarı</th></tr></thead><tbody>");
            foreach (var g in report.GroupPerformances)
                sb.AppendLine($"<tr><td>{g.GroupName}</td><td class='text-center'><span class='badge {(g.PercentageScore >= 80 ? "bg-success" : g.PercentageScore >= 60 ? "bg-warning" : "bg-danger")}'>{g.PercentageScore:F1}%</span></td></tr>");
            sb.AppendLine("</tbody></table></div>");
        }

        if (report.Strengths.Any() || report.Weaknesses.Any())
        {
            sb.AppendLine("<div class='card'><div class='card-header'>Güçlü ve Zayıf Yönler</div>");
            if (report.Strengths.Any())
            {
                sb.AppendLine("<h3 style='color:#198754;margin:10px 12px 5px;'>Güçlü Yönler</h3><ul style='margin:0 12px 10px;'>");
                foreach (var s in report.Strengths.Take(5))
                    sb.AppendLine($"<li><span class='badge bg-success'>{s.PercentageScore:F0}%</span> {s.QuestionText}</li>");
                sb.AppendLine("</ul>");
            }
            if (report.Weaknesses.Any())
            {
                sb.AppendLine("<h3 style='color:#dc3545;margin:10px 12px 5px;'>Geliştirilmeli</h3><ul style='margin:0 12px 10px;'>");
                foreach (var w in report.Weaknesses.Take(5))
                    sb.AppendLine($"<li><span class='badge bg-danger'>{w.PercentageScore:F0}%</span> {w.QuestionText}</li>");
                sb.AppendLine("</ul>");
            }
            sb.AppendLine("</div>");
        }

        if (report.RecentEvaluations.Any())
        {
            sb.AppendLine("<div class='page-break'></div>");
            sb.AppendLine("<div class='card'><div class='card-header'>Son Değerlendirmeler</div>");
            sb.AppendLine("<table><thead><tr><th>Tarih</th><th>Proje</th><th>Kontrol Listesi</th><th class='text-center'>Puan</th><th class='text-center'>Kartlar</th><th>Personel</th></tr></thead><tbody>");
            foreach (var eval in report.RecentEvaluations.Take(20))
            {
                var cards = "";
                if (eval.YellowCards > 0) cards += $"<span class='badge bg-warning'>{eval.YellowCards}</span> ";
                if (eval.RedCards > 0) cards += $"<span class='badge bg-danger'>{eval.RedCards}</span>";
                if (string.IsNullOrEmpty(cards)) cards = "-";
                sb.AppendLine($"<tr><td>{eval.EvaluationDate?.ToString("dd.MM.yyyy") ?? "-"}</td><td>{eval.ProjectName}</td><td>{eval.ChecklistName}</td><td class='text-center'><span class='badge {(eval.ScorePercentage >= 80 ? "bg-success" : eval.ScorePercentage >= 60 ? "bg-warning" : "bg-danger")}'>{eval.ScorePercentage:F1}%</span></td><td class='text-center'>{cards}</td><td>{eval.PersonnelName ?? "-"}</td></tr>");
            }
            sb.AppendLine("</tbody></table></div>");
        }

        sb.AppendLine($"<p style='text-align:right;font-size:9pt;color:#999;margin-top:20px;'>Oluşturulma: {TurkeyTime.Now:dd.MM.yyyy HH:mm}</p>");
        sb.AppendLine("</body></html>");
        return sb.ToString();
    }

    // =============================================
    // GÖLGE MÜŞTERİ ARAMALARI
    // =============================================

    [HttpGet("gm-aramalar")]
    public async Task<IActionResult> GetGmAramalar([FromQuery] int? donemId)
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var result = await _cpDataService.GetGmAramalarAsync(customerId.Value, donemId);
        return Ok(result);
    }

    [HttpGet("gm-donemler")]
    public async Task<IActionResult> GetGmDonemler()
    {
        var customerId = GetCustomerId();
        if (customerId == null)
            return Unauthorized(new { message = "Müşteri bilgisi bulunamadı." });

        var donemler = await _cpDataService.GetGmDonemlerAsync(customerId.Value);
        return Ok(donemler);
    }

}
