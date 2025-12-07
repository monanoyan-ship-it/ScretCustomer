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

    public MyAssignmentsController(
        IAssignmentService assignmentService,
        IFieldWorkerService fieldWorkerService,
        ILogger<MyAssignmentsController> logger)
    {
        _assignmentService = assignmentService;
        _fieldWorkerService = fieldWorkerService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            
            // Get field worker by user id
            var fieldWorker = await _fieldWorkerService.GetByUserIdAsync(userId);
            
            if (fieldWorker == null)
            {
                _logger.LogWarning("Field worker not found for user {UserId}", userId);
                return RedirectToAction("AccessDenied", "Account");
            }

            // Get assignments for this field worker
            var assignments = await _assignmentService.GetByFieldWorkerIdAsync(fieldWorker.Id);
            
            ViewData["FieldWorkerName"] = $"{fieldWorker.FirstName} {fieldWorker.LastName}";
            
            return View(assignments);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading my assignments");
            TempData["ErrorMessage"] = "Atamalar yüklenirken bir hata oluştu.";
            return View(new List<Core.Entities.Assignment>());
        }
    }
}
