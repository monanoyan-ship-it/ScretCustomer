using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.API.ViewModels;
using SecretCustomer.Core.DTOs.Assignment;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers;

[Authorize]
public class AssignmentsController : Controller
{
    private readonly IAssignmentService _assignmentService;
    private readonly IProjectService _projectService;
    private readonly IBranchService _branchService;
    private readonly IChecklistService _checklistService;
    private readonly IUserService _userService;
    private readonly ILogger<AssignmentsController> _logger;

    public AssignmentsController(
        IAssignmentService assignmentService,
        IProjectService projectService,
        IBranchService branchService,
        IChecklistService checklistService,
        IUserService userService,
        ILogger<AssignmentsController> logger)
    {
        _assignmentService = assignmentService;
        _projectService = projectService;
        _branchService = branchService;
        _checklistService = checklistService;
        _userService = userService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            IEnumerable<AssignmentDto> assignments;

            if (Guid.TryParse(userIdClaim, out Guid userId))
            {
                assignments = await _assignmentService.GetByUserIdAsync(userId);
            }
            else
            {
                assignments = new List<AssignmentDto>();
            }

            var viewModel = new AssignmentIndexViewModel
            {
                Assignments = assignments.ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading assignments");
            TempData["Error"] = "Atamalar yüklenirken bir hata oluştu.";
            return View(new AssignmentIndexViewModel());
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create()
    {
        try
        {
            var projects = await _projectService.GetAllAsync(false);
            var branches = await _branchService.GetAllAsync();
            var checklists = await _checklistService.GetAllAsync();
            var evaluators = await _userService.GetByRoleAsync(UserRole.Evaluator);

            var viewModel = new AssignmentCreateViewModel
            {
                AvailableProjects = projects.ToList(),
                AvailableBranches = branches.ToList(),
                AvailableChecklists = checklists.ToList(),
                AvailableEvaluators = evaluators.ToList()
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading create assignment form");
            TempData["Error"] = "Form yüklenirken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssignmentCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            await LoadDropdowns(model);
            return View(model);
        }

        try
        {
            var dto = new CreateAssignmentDto
            {
                ProjectId = model.ProjectId,
                BranchId = model.BranchId,
                ChecklistId = model.ChecklistId,
                AssignedUserId = model.AssignedUserId,
                DueDate = model.DueDate
            };

            await _assignmentService.CreateAsync(dto);
            TempData["Success"] = "Atama başarıyla oluşturuldu.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating assignment");
            ModelState.AddModelError(string.Empty, "Atama oluşturulurken bir hata oluştu.");
            await LoadDropdowns(model);
            return View(model);
        }
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _assignmentService.DeleteAsync(id);
            TempData["Success"] = "Atama başarıyla silindi.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting assignment {Id}", id);
            TempData["Error"] = "Atama silinirken bir hata oluştu.";
            return RedirectToAction(nameof(Index));
        }
    }

    private async Task LoadDropdowns(AssignmentCreateViewModel model)
    {
        var projects = await _projectService.GetAllAsync(false);
        var branches = await _branchService.GetAllAsync();
        var checklists = await _checklistService.GetAllAsync();
        var evaluators = await _userService.GetByRoleAsync(UserRole.Evaluator);

        model.AvailableProjects = projects.ToList();
        model.AvailableBranches = branches.ToList();
        model.AvailableChecklists = checklists.ToList();
        model.AvailableEvaluators = evaluators.ToList();
    }
}
