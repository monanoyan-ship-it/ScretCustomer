using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Services.Helpers;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/customer/[controller]")]
public class CustomerPortalAuthController : ControllerBase
{
    private readonly ICustomerPersonnelService _personnelService;
    private readonly IAppSettingsService _appSettingsService;
    private readonly JwtHelper _jwtHelper;
    private readonly ILocalizationService _localizationService;
    private readonly IAuditLogService _auditLogService;

    public CustomerPortalAuthController(
        ICustomerPersonnelService personnelService,
        IAppSettingsService appSettingsService,
        JwtHelper jwtHelper,
        ILocalizationService localizationService,
        IAuditLogService auditLogService)
    {
        _personnelService = personnelService;
        _appSettingsService = appSettingsService;
        _jwtHelper = jwtHelper;
        _localizationService = localizationService;
        _auditLogService = auditLogService;
    }

    public class CustomerLoginDto
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] CustomerLoginDto loginDto)
    {
        var isDemoMode = await _appSettingsService.IsDemoModeAsync();

        if (string.IsNullOrEmpty(loginDto.Username) || string.IsNullOrEmpty(loginDto.Password))
        {
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Account.LoginRequired") });
        }

        try
        {
            var personnel = await _personnelService.AuthenticateAsync(loginDto.Username, loginDto.Password);

            if (personnel == null)
            {
                await _auditLogService.LogLoginAsync(0, $"[Müşteri] {loginDto.Username}", false, "Geçersiz kullanıcı adı veya şifre");

                if (isDemoMode)
                {
                    return Unauthorized(new {
                        message = await _localizationService.GetResourceAsync("Auth.InvalidCredentials"),
                        debug = new {
                            hint = "Kullanıcı bulunamadı veya şifre yanlış",
                            username = loginDto.Username,
                            tip = "Seed data kullanıcıları: ahmet.yilmaz, ayse.demir, mehmet.kaya (şifre: Customer@123)"
                        }
                    });
                }
                return Unauthorized(new { message = await _localizationService.GetResourceAsync("Auth.InvalidCredentials") });
            }

            // Generate JWT token for customer personnel
            var token = _jwtHelper.GenerateCustomerPersonnelToken(personnel);

            // Kullanıcının kayıtlı dil tercihini uygula
            _localizationService.ApplyUserLanguagePreference(personnel.Id);

            await _auditLogService.LogLoginAsync(personnel.Id, $"[Müşteri] {personnel.Username}", true);

            return Ok(new
            {
                token,
                user = new
                {
                    id = personnel.Id,
                    username = personnel.Username,
                    fullName = personnel.FullName,
                    email = personnel.Email,
                    role = CustomerPersonnelRoles.GetById(personnel.RoleId)?.SystemName ?? "",
                    customerId = personnel.CustomerId,
                    customerName = personnel.Customer?.CompanyName ?? ""
                }
            });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"[Müşteri] Login hatası: {loginDto.Username}", "Auth", ex);

            if (isDemoMode)
            {
                return StatusCode(500, new {
                    message = await _localizationService.GetResourceAsync("Auth.LoginError"),
                    debug = new {
                        error = ex.Message,
                        stackTrace = ex.StackTrace,
                        innerException = ex.InnerException?.Message
                    }
                });
            }
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Auth.LoginError") });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userType = User.FindFirst("UserType")?.Value;
        if (userType != "CustomerPersonnel")
        {
            return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortalAuth.CustomerPersonnelOnly") });
        }

        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var customerId = User.FindFirst("CustomerId")?.Value;
        var customerName = User.FindFirst("CustomerName")?.Value;
        var fullName = User.FindFirst("FullName")?.Value;

        return Ok(new
        {
            id = userId,
            username,
            email,
            fullName,
            role,
            customerId,
            customerName
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value ?? "Unknown";
        int.TryParse(userIdClaim, out var userId);

        await _auditLogService.LogLogoutAsync(userId, $"[Müşteri] {username}");

        return Ok(new { message = await _localizationService.GetResourceAsync("Auth.LogoutSuccess") });
    }
}
