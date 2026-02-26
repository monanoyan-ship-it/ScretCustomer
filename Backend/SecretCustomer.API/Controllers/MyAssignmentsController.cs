using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "FieldWorker")]
public class MyAssignmentsController : Controller
{
    private readonly IAssignmentService _assignmentService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILocalizationService _localizationService;

    public MyAssignmentsController(
        IAssignmentService assignmentService,
        IAuditLogService auditLogService,
        ILocalizationService localizationService)
    {
        _assignmentService = assignmentService;
        _auditLogService = auditLogService;
        _localizationService = localizationService;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var userName = User.FindFirstValue(ClaimTypes.Name) ?? await _localizationService.GetResourceAsync("Common.User");

            // Get assignments for this user
            var assignments = await _assignmentService.GetByUserIdAsync(userId);

            ViewData["FieldWorkerName"] = userName;

            return View(assignments);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync("Error loading my assignments", "MyAssignments", ex);
            TempData["ErrorMessage"] = await _localizationService.GetResourceAsync("Assignment.LoadError");
            return View(new List<Core.DTOs.Assignment.AssignmentDto>());
        }
    }
}
