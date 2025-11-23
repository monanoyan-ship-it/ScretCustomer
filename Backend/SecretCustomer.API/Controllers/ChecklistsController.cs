using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.API.ViewModels;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin")]
public class ChecklistsController : Controller
{
    private readonly IChecklistService _checklistService;
    private readonly ILogger<ChecklistsController> _logger;

    public ChecklistsController(IChecklistService checklistService, ILogger<ChecklistsController> logger)
    {
        _checklistService = checklistService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var checklists = await _checklistService.GetAllAsync();
            var viewModel = new ChecklistIndexViewModel
            {
                Checklists = checklists.ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading checklists");
            TempData["Error"] = "Kontrol listeleri yüklenirken bir hata oluştu.";
            return View(new ChecklistIndexViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> View(Guid id)
    {
        try
        {
            var checklist = await _checklistService.GetByIdAsync(id);
            if (checklist == null)
            {
                TempData["Error"] = "Kontrol listesi bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            return View(checklist);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error viewing checklist {Id}", id);
            TempData["Error"] = "Kontrol listesi görüntülenirken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Clone(Guid id)
    {
        try
        {
            var clonedChecklist = await _checklistService.CloneChecklistAsync(id, "Copy");
            TempData["Success"] = "Kontrol listesi başarıyla klonlandı.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cloning checklist {Id}", id);
            TempData["Error"] = "Kontrol listesi klonlanırken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }
    }
}
