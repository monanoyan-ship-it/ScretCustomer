using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.API.ViewModels;
using SecretCustomer.Core.DTOs.Project;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin")]
public class ProjectsController : Controller
{
    private readonly IProjectService _projectService;
    private readonly IChecklistService _checklistService;
    private readonly ILogger<ProjectsController> _logger;

    public ProjectsController(
        IProjectService projectService,
        IChecklistService checklistService,
        ILogger<ProjectsController> logger)
    {
        _projectService = projectService;
        _checklistService = checklistService;
        _logger = logger;
    }

    public async Task<IActionResult> Index(bool includeInactive = false)
    {
        try
        {
            var projects = await _projectService.GetAllAsync(includeInactive);
            var viewModel = new ProjectIndexViewModel
            {
                Projects = projects.ToList(),
                IncludeInactive = includeInactive
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading projects");
            TempData["Error"] = "Projeler yüklenirken bir hata oluştu.";
            return View(new ProjectIndexViewModel());
        }
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        try
        {
            var checklists = await _checklistService.GetAllAsync();
            var viewModel = new ProjectCreateEditViewModel
            {
                AvailableChecklists = checklists.ToList()
            };

            return View("CreateEdit", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create project form");
            TempData["Error"] = "Form yüklenirken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        try
        {
            var project = await _projectService.GetByIdAsync(id);
            if (project == null)
            {
                TempData["Error"] = "Proje bulunamadı.";
                return RedirectToAction(nameof(Index));
            }

            var checklists = await _checklistService.GetAllAsync();
            var viewModel = new ProjectCreateEditViewModel
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                ChecklistId = project.ChecklistId,
                StartDate = project.StartDate,
                EndDate = project.EndDate,
                AvailableChecklists = checklists.ToList()
            };

            return View("CreateEdit", viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading project {Id}", id);
            TempData["Error"] = "Proje yüklenirken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(ProjectCreateEditViewModel model)
    {
        if (!ModelState.IsValid)
        {
            var checklists = await _checklistService.GetAllAsync();
            model.AvailableChecklists = checklists.ToList();
            return View("CreateEdit", model);
        }

        try
        {
            var dto = new CreateProjectDto
            {
                Name = model.Name,
                Description = model.Description,
                ChecklistId = model.ChecklistId,
                StartDate = model.StartDate,
                EndDate = model.EndDate
            };

            if (model.Id.HasValue)
            {
                await _projectService.UpdateAsync(model.Id.Value, dto);
                TempData["Success"] = "Proje başarıyla güncellendi.";
            }
            else
            {
                await _projectService.CreateAsync(dto);
                TempData["Success"] = "Proje başarıyla oluşturuldu.";
            }

            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving project");
            ModelState.AddModelError(string.Empty, "Proje kaydedilirken bir hata oluştu.");
            var checklists = await _checklistService.GetAllAsync();
            model.AvailableChecklists = checklists.ToList();
            return View("CreateEdit", model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Close(Guid id)
    {
        try
        {
            await _projectService.CloseProjectAsync(id);
            TempData["Success"] = "Proje başarıyla kapatıldı.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing project {Id}", id);
            TempData["Error"] = "Proje kapatılırken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _projectService.DeleteAsync(id);
            TempData["Success"] = "Proje başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting project {Id}", id);
            TempData["Error"] = "Proje silinirken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }
    }
}
