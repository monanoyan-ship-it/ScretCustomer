using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.API.ViewModels;
using SecretCustomer.Core.DTOs.Auth;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly ILogger<AccountController> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly IAuditLogService _auditLogService;

    public AccountController(
        IAuthService authService,
        ILogger<AccountController> logger,
        ILocalizationService localizationService,
        IAuditLogService auditLogService)
    {
        _authService = authService;
        _logger = logger;
        _localizationService = localizationService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated ?? false)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var loginDto = new LoginDto
            {
                Username = model.Username,
                Password = model.Password
            };

            var result = await _authService.LoginAsync(loginDto);

            if (result == null)
            {
                ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Auth.InvalidCredentials"));
                return View(model);
            }

            // Create claims for cookie authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, result.UserId.ToString()),
                new Claim(ClaimTypes.Name, result.FullName),
                new Claim(ClaimTypes.Email, result.Username), // Using username as email
                new Claim(ClaimTypes.Role, result.Role),
                new Claim("Username", result.Username),
                new Claim("FullName", result.FullName),
                new Claim("MustChangePassword", result.MustChangePassword.ToString())
            };

            // CustomerPersonnel ise ek claim'ler ekle
            if (result.CustomerId.HasValue)
            {
                claims.Add(new Claim("UserType", "CustomerPersonnel"));
                claims.Add(new Claim("CustomerId", result.CustomerId.Value.ToString()));
                if (!string.IsNullOrEmpty(result.CustomerName))
                    claims.Add(new Claim("CustomerName", result.CustomerName));
            }
            else
            {
                claims.Add(new Claim("UserType", "User"));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // Kullanıcının kayıtlı dil tercihini uygula
            _localizationService.ApplyUserLanguagePreference(result.UserId);

            // Audit Log - Başarılı giriş
            await _auditLogService.LogLoginAsync(result.UserId, result.Username, success: true);

            _logger.LogInformation("User {Username} logged in successfully", model.Username);

            // Şifre değiştirmesi gereken kullanıcıyı Profile sayfasına yönlendir
            if (result.MustChangePassword)
            {
                TempData["MustChangePassword"] = true;
                TempData["Info"] = await _localizationService.GetResourceAsync("Account.MustChangePassword", defaultValue: "İlk giriş veya şifre sıfırlama sonrası şifrenizi değiştirmeniz gerekmektedir.");
                return RedirectToAction("Index", "Profile");
            }

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            {
                return Redirect(model.ReturnUrl);
            }

            return RedirectToAction("Index", "Home");
        }
        catch (UnauthorizedAccessException)
        {
            // Audit Log - Başarısız giriş
            await _auditLogService.LogLoginAsync(0, model.Username, false, "Geçersiz kullanıcı adı veya şifre");

            _logger.LogWarning("Failed login attempt for user {Username}", model.Username);
            ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Auth.InvalidCredentials"));
            return View(model);
        }
        catch (Exception ex)
        {
            // Audit Log - Hata
            await _auditLogService.LogErrorAsync($"Login hatası: {model.Username}", "Account", ex);

            _logger.LogError(ex, "Error during login for user {Username}", model.Username);
            ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Mvc.Account.LoginError"));
            return View(model);
        }
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        // Audit Log - Çıkış
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        if (int.TryParse(userIdClaim, out var userId))
        {
            await _auditLogService.LogLogoutAsync(userId, userName);
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        _logger.LogInformation("User logged out");
        return RedirectToAction("Login", "Account");
    }

    [HttpGet]
    public IActionResult AccessDenied()
    {
        return View();
    }

    // Şifremi Unuttum
    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _authService.ForgotPasswordAsync(model);

            if (result.Success)
            {
                // Development ortamında token'ı göster
                if (!string.IsNullOrEmpty(result.ResetToken))
                {
                    TempData["ResetToken"] = result.ResetToken;
                    TempData["Email"] = model.Email;
                }

                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("ForgotPasswordConfirmation");
            }

            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during forgot password for email {Email}", model.Email);
            ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Mvc.Account.OperationError"));
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ForgotPasswordConfirmation()
    {
        return View();
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPassword(string? token = null, string? email = null)
    {
        if (string.IsNullOrEmpty(token) && TempData["ResetToken"] != null)
        {
            token = TempData["ResetToken"]?.ToString();
            email = TempData["Email"]?.ToString();
        }

        var model = new ResetPasswordDto
        {
            Token = token ?? string.Empty,
            Email = email ?? string.Empty
        };

        return View(model);
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        try
        {
            var result = await _authService.ResetPasswordAsync(model);

            if (result.Success)
            {
                TempData["SuccessMessage"] = result.Message;
                return RedirectToAction("ResetPasswordConfirmation");
            }

            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during password reset for email {Email}", model.Email);
            ModelState.AddModelError(string.Empty, await _localizationService.GetResourceAsync("Mvc.Account.OperationError"));
            return View(model);
        }
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult ResetPasswordConfirmation()
    {
        return View();
    }
}
