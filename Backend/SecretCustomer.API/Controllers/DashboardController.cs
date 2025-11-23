using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.API.ViewModels;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers;

[Authorize]
public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;
    private readonly ILogger<DashboardController> _logger;

    public DashboardController(IDashboardService dashboardService, ILogger<DashboardController> logger)
    {
        _dashboardService = dashboardService;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            var stats = await _dashboardService.GetAdminDashboardAsync();

            var viewModel = new DashboardViewModel
            {
                TotalEvaluations = stats.TotalEvaluations,
                AverageScore = stats.AverageScore,
                PercentageChange = stats.PercentageChange,
                TopBranches = stats.TopBranches,
                BottomBranches = stats.BottomBranches
            };

            return View(viewModel);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard");
            TempData["Error"] = "Dashboard verileri yüklenirken bir hata oluştu.";
            return View(new DashboardViewModel());
        }
    }
}
