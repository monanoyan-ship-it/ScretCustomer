using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin")]
public class AuditLogsController : Controller
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILocalizationService _localizationService;

    public AuditLogsController(IAuditLogService auditLogService, ILocalizationService localizationService)
    {
        _auditLogService = auditLogService;
        _localizationService = localizationService;
    }

    public async Task<IActionResult> Index()
    {
        ViewBag.Title = await _localizationService.GetResourceAsync("AuditLogs.Title", defaultValue: "Sistem Logları");
        return View();
    }
}
