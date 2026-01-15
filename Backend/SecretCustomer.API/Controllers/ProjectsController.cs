using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Proje yönetimi MVC controller
/// </summary>
[Authorize(Roles = "Admin,QualitySpecialist")]
public class ProjectsController : Controller
{
    /// <summary>
    /// Proje yönetimi sayfası (Liste + Create/Edit/Detail modalleri)
    /// </summary>
    public IActionResult Index()
    {
        return View();
    }
}
