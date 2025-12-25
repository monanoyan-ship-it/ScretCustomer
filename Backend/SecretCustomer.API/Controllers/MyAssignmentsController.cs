using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Core.Enums;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "FieldWorker")]
public class MyAssignmentsController : Controller
{
    private readonly IAssignmentService _assignmentService;
    private readonly IFieldWorkerService _fieldWorkerService;
    private readonly ILogger<MyAssignmentsController> _logger;
    private readonly ILocalizationService _localizationService;

    public MyAssignmentsController(
        IAssignmentService assignmentService,
        IFieldWorkerService fieldWorkerService,
        ILogger<MyAssignmentsController> logger,
        ILocalizationService localizationService)
    {
        _assignmentService = assignmentService;
        _fieldWorkerService = fieldWorkerService;
        _logger = logger;
        _localizationService = localizationService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? await _localizationService.GetResourceAsync("Common.User");

            // Get field worker by user id
            var fieldWorker = await _fieldWorkerService.GetByUserIdAsync(userId);

            if (fieldWorker == null)
            {
                _logger.LogWarning("Field worker not found for user {UserId}", userId);
                // Show empty list with informative message instead of redirecting
                ViewData["FieldWorkerName"] = userName;
                TempData["ErrorMessage"] = await _localizationService.GetResourceAsync("Mvc.MyAssignments.FieldWorkerNotFound");
                return View(new List<Core.DTOs.Assignment.AssignmentDto>());
            }

            // Get assignments for this field worker
            var assignments = await _assignmentService.GetByFieldWorkerIdAsync(fieldWorker.Id);

            ViewData["FieldWorkerName"] = $"{fieldWorker.FirstName} {fieldWorker.LastName}";

            return View(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading my assignments");
            TempData["ErrorMessage"] = await _localizationService.GetResourceAsync("Mvc.MyAssignments.LoadError");
            return View(new List<Core.DTOs.Assignment.AssignmentDto>());
        }
    }
}
