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
    /// <param name="typeId">Proje tipi ID'si (opsiyonel - menüden tipe göre filtreleme için)</param>
    public IActionResult Index(int? typeId = null)
    {
        ViewBag.ProjectTypeId = typeId;
        return View();
    }
}
