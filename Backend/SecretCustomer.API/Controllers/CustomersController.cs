using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.API.Controllers;

[Authorize(Roles = "Admin")]
public class CustomersController : Controller
{
    private readonly ICustomerService _customerService;

    public CustomersController(ICustomerService customerService)
    {
        _customerService = customerService;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Dealers(int id)
    {
        ViewBag.CustomerId = id;
        return View();
    }

    public async Task<IActionResult> Personnel(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
            return NotFound();

        ViewBag.CustomerId = id;
        ViewBag.CustomerName = customer.CompanyName;
        return View();
    }

    public async Task<IActionResult> Organizations(int id)
    {
        var customer = await _customerService.GetByIdAsync(id);
        if (customer == null)
            return NotFound();

        ViewBag.CustomerId = id;
        ViewBag.CustomerName = customer.CompanyName;
        return View();
    }
}
