using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SecretCustomer.API.Controllers;

/// <summary>
/// Halka açık eğitim video izleme controller (login gerektirmez)
/// </summary>
[AllowAnonymous]
public class TrainingController : Controller
{
    /// <summary>
    /// Dış katılımcı video izleme sayfası (token ile erişim)
    /// </summary>
    [Route("Training/External/{token}")]
    public IActionResult External(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return NotFound();
        }

        ViewBag.Token = token;
        return View();
    }
}
