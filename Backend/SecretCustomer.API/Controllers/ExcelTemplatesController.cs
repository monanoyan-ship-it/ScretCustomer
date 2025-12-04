using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.API.DTOs;
using System.Text.Json;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin")]
public class ExcelTemplatesController : Controller
{
    private readonly IExcelTemplateService _excelTemplateService;
    private readonly IHttpClientFactory _httpClientFactory;

    public ExcelTemplatesController(
        IExcelTemplateService excelTemplateService,
        IHttpClientFactory httpClientFactory)
    {
        _excelTemplateService = excelTemplateService;
        _httpClientFactory = httpClientFactory;
    }

    // GET: /ExcelTemplates/Index
    public async Task<IActionResult> Index()
    {
        var templates = await _excelTemplateService.GetAllAsync(includeInactive: false);
        return View(templates);
    }

    // GET: /ExcelTemplates/Create
    public IActionResult Create()
    {
        return View();
    }

    // GET: /ExcelTemplates/Edit/5
    public async Task<IActionResult> Edit(Guid id)
    {
        var template = await _excelTemplateService.GetByIdAsync(id, includeColumns: true);

        if (template == null)
        {
            TempData["Error"] = "Şablon bulunamadı";
            return RedirectToAction(nameof(Index));
        }

        return View(template);
    }

    // POST: /ExcelTemplates/Delete/5
    [HttpPost]
    public async Task<IActionResult> Delete(Guid id)
    {
        var result = await _excelTemplateService.DeleteAsync(id);

        if (result)
        {
            TempData["Success"] = "Şablon başarıyla silindi";
        }
        else
        {
            TempData["Error"] = "Şablon silinirken bir hata oluştu";
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: /ExcelTemplates/Export/5
    public async Task<IActionResult> Export(Guid id)
    {
        try
        {
            var excelBytes = await _excelTemplateService.GenerateTemplateExcelAsync(id);
            var template = await _excelTemplateService.GetByIdAsync(id);

            var filename = $"{template?.Name.Replace(" ", "_") ?? "template"}_sablon.xlsx";

            return File(
                excelBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                filename);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Excel oluşturulurken hata: {ex.Message}";
            return RedirectToAction(nameof(Index));
        }
    }

    // GET: /ExcelTemplates/Import/5
    public async Task<IActionResult> Import(Guid id)
    {
        var template = await _excelTemplateService.GetByIdAsync(id, includeColumns: true);

        if (template == null)
        {
            TempData["Error"] = "Şablon bulunamadı";
            return RedirectToAction(nameof(Index));
        }

        return View(template);
    }

    // POST: /ExcelTemplates/ProcessImport/5
    [HttpPost]
    public async Task<IActionResult> ProcessImport(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            TempData["Error"] = "Lütfen bir dosya seçin";
            return RedirectToAction(nameof(Import), new { id });
        }

        if (!file.FileName.EndsWith(".xlsx") && !file.FileName.EndsWith(".xls"))
        {
            TempData["Error"] = "Sadece Excel dosyaları (.xlsx, .xls) yüklenebilir";
            return RedirectToAction(nameof(Import), new { id });
        }

        try
        {
            // Read file content
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileContent = memoryStream.ToArray();

            // Parse Excel
            var result = await _excelTemplateService.ParseExcelAsync(id, fileContent);

            // Store result in TempData for the result page
            TempData["ParseResult"] = JsonSerializer.Serialize(result);
            TempData["TemplateId"] = id.ToString();

            return RedirectToAction(nameof(ImportResult));
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Excel işlenirken hata: {ex.Message}";
            return RedirectToAction(nameof(Import), new { id });
        }
    }

    // GET: /ExcelTemplates/ImportResult
    public IActionResult ImportResult()
    {
        if (TempData["ParseResult"] == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var resultJson = TempData["ParseResult"]?.ToString();
        var templateId = TempData["TemplateId"]?.ToString();

        if (string.IsNullOrEmpty(resultJson))
        {
            return RedirectToAction(nameof(Index));
        }

        var result = JsonSerializer.Deserialize<ExcelParseResult>(resultJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        ViewBag.TemplateId = templateId;

        return View(result);
    }
}
