using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
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
    private readonly ILogger<CustomerPortalAuthController> _logger;
    private readonly ILocalizationService _localizationService;

    public CustomerPortalAuthController(
        ICustomerPersonnelService personnelService,
        IAppSettingsService appSettingsService,
        JwtHelper jwtHelper,
        ILogger<CustomerPortalAuthController> logger,
        ILocalizationService localizationService)
    {
        _personnelService = personnelService;
        _appSettingsService = appSettingsService;
        _jwtHelper = jwtHelper;
        _logger = logger;
        _localizationService = localizationService;
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
            return BadRequest(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortalAuth.UsernamePasswordRequired") });
        }

        try
        {
            _logger.LogInformation("[CustomerPortal] Login attempt for: {Username}", loginDto.Username);

            var personnel = await _personnelService.AuthenticateAsync(loginDto.Username, loginDto.Password);

            if (personnel == null)
            {
                _logger.LogWarning("[CustomerPortal] Failed login for: {Username} - User not found or password incorrect", loginDto.Username);

                if (isDemoMode)
                {
                    return Unauthorized(new {
                        message = await _localizationService.GetResourceAsync("Api.CustomerPortalAuth.InvalidCredentials"),
                        debug = new {
                            hint = "Kullanıcı bulunamadı veya şifre yanlış",
                            username = loginDto.Username,
                            tip = "Seed data kullanıcıları: ahmet.yilmaz, ayse.demir, mehmet.kaya (şifre: Customer@123)"
                        }
                    });
                }
                return Unauthorized(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortalAuth.InvalidCredentials") });
            }

            _logger.LogInformation("[CustomerPortal] User found: {Username}, CustomerId: {CustomerId}",
                personnel.Username, personnel.CustomerId);

            // Generate JWT token for customer personnel
            var token = _jwtHelper.GenerateCustomerPersonnelToken(personnel);

            _logger.LogInformation("[CustomerPortal] Login successful for: {Username}", loginDto.Username);

            return Ok(new
            {
                token,
                user = new
                {
                    id = personnel.Id,
                    username = personnel.Username,
                    fullName = personnel.FullName,
                    email = personnel.Email,
                    role = personnel.Role.ToString(),
                    customerId = personnel.CustomerId,
                    customerName = personnel.Customer?.CompanyName ?? ""
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[CustomerPortal] Error during login for {Username}: {Message}",
                loginDto.Username, ex.Message);

            if (isDemoMode)
            {
                return StatusCode(500, new {
                    message = await _localizationService.GetResourceAsync("Api.CustomerPortalAuth.LoginError"),
                    debug = new {
                        error = ex.Message,
                        stackTrace = ex.StackTrace,
                        innerException = ex.InnerException?.Message
                    }
                });
            }
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Api.CustomerPortalAuth.LoginError") });
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
        _logger.LogInformation("Customer personnel logged out");
        return Ok(new { message = await _localizationService.GetResourceAsync("Api.CustomerPortalAuth.LogoutSuccess") });
    }
}
