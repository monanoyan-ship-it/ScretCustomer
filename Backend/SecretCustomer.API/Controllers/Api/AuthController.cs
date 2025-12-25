using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Auth;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Services.Helpers;

namespace SecretCustomer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly JwtHelper _jwtHelper;
    private readonly ILogger<AuthController> _logger;
    private readonly ILocalizationService _localizationService;
    private readonly IAuditLogService _auditLogService;

    public AuthController(
        IAuthService authService,
        JwtHelper jwtHelper,
        ILogger<AuthController> logger,
        ILocalizationService localizationService,
        IAuditLogService auditLogService)
    {
        _authService = authService;
        _jwtHelper = jwtHelper;
        _logger = logger;
        _localizationService = localizationService;
        _auditLogService = auditLogService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var user = await _authService.LoginAsync(loginDto);

            if (user == null)
            {
                return Unauthorized(new { message = await _localizationService.GetResourceAsync("Auth.InvalidCredentials") });
            }

            // Generate JWT token
            // Create a minimal User entity for token generation
            var userEntity = new User
            {
                Id = user.UserId,
                Username = user.Username,
                Email = user.Username, // Using username as email fallback
                Role = Enum.Parse<Core.Enums.UserRole>(user.Role)
            };

            var token = _jwtHelper.GenerateToken(userEntity);

            _logger.LogInformation("User {Username} logged in successfully", loginDto.Username);

            return Ok(new
            {
                token,
                user = new
                {
                    id = user.UserId,
                    username = user.Username,
                    fullName = user.FullName,
                    role = user.Role
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for user {Username}", loginDto.Username);
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Auth.LoginError") });
        }
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult GetCurrentUser()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        return Ok(new
        {
            id = userId,
            username,
            role
        });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        // JWT is stateless, so just return success
        // Client will delete the token
        _logger.LogInformation("User logged out");
        return Ok(new { message = await _localizationService.GetResourceAsync("Auth.LogoutSuccess") });
    }

    // ===== IMPERSONATION (Video 17 - Firma Olarak Görüntüle) =====

    /// <summary>
    /// Impersonation için müşteri listesini getirir
    /// </summary>
    [HttpGet("impersonation/customers")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetCustomersForImpersonation()
    {
        try
        {
            var customers = await _authService.GetCustomersForImpersonationAsync();
            return Ok(customers);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading customers for impersonation");
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Auth.CustomerListError") });
        }
    }

    /// <summary>
    /// Müşteri olarak görüntülemeye başlar
    /// </summary>
    [HttpPost("impersonation/start")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> StartImpersonation([FromBody] StartImpersonationDto dto)
    {
        try
        {
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { message = await _localizationService.GetResourceAsync("Auth.UserNotFound") });
            }

            var result = await _authService.StartImpersonationAsync(userId, dto.CustomerId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            _logger.LogInformation("User {UserId} started impersonating customer {CustomerId}",
                userId, dto.CustomerId);

            // Audit Log - Impersonation başlatıldı
            await _auditLogService.LogWarningAsync(
                $"Firma olarak görüntüleme başlatıldı: Müşteri ID {dto.CustomerId}",
                "Impersonation");

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting impersonation");
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Auth.ImpersonationStartError") });
        }
    }

    /// <summary>
    /// Müşteri olarak görüntülemeyi sonlandırır
    /// </summary>
    [HttpPost("impersonation/end")]
    [Authorize]
    public async Task<IActionResult> EndImpersonation()
    {
        try
        {
            // Orijinal kullanıcı ID'sini claim'den al
            var originalUserIdClaim = User.FindFirst("OriginalUserId")?.Value;
            if (string.IsNullOrEmpty(originalUserIdClaim))
            {
                // Eğer OriginalUserId yoksa, normal kullanıcı olarak devam et
                var currentUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(currentUserId) && int.TryParse(currentUserId, out var userId))
                {
                    originalUserIdClaim = currentUserId;
                }
                else
                {
                    return BadRequest(new { message = await _localizationService.GetResourceAsync("Auth.ImpersonationNotActive") });
                }
            }

            if (!int.TryParse(originalUserIdClaim, out var originalUserId))
            {
                return BadRequest(new { message = await _localizationService.GetResourceAsync("Auth.InvalidUserId") });
            }

            var result = await _authService.EndImpersonationAsync(originalUserId);

            if (!result.Success)
            {
                return BadRequest(new { message = result.Message });
            }

            _logger.LogInformation("User {UserId} ended impersonation", originalUserId);

            // Audit Log - Impersonation sonlandırıldı
            await _auditLogService.LogInfoAsync(
                "Firma olarak görüntüleme sonlandırıldı",
                "Impersonation");

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ending impersonation");
            return StatusCode(500, new { message = await _localizationService.GetResourceAsync("Auth.ImpersonationEndError") });
        }
    }

    /// <summary>
    /// Aktif impersonation bilgisini getirir
    /// </summary>
    [HttpGet("impersonation/status")]
    [Authorize]
    public IActionResult GetImpersonationStatus()
    {
        var isImpersonating = User.FindFirst("IsImpersonating")?.Value == "true";
        var customerId = User.FindFirst("ImpersonatedCustomerId")?.Value;
        var customerName = User.FindFirst("ImpersonatedCustomerName")?.Value;
        var originalUserId = User.FindFirst("OriginalUserId")?.Value;
        var originalUserName = User.FindFirst("OriginalUserName")?.Value;
        var originalRole = User.FindFirst("OriginalRole")?.Value;

        return Ok(new ImpersonationInfoDto
        {
            IsImpersonating = isImpersonating,
            OriginalUserId = !string.IsNullOrEmpty(originalUserId) && int.TryParse(originalUserId, out var ouid) ? ouid : null,
            OriginalUserName = originalUserName,
            OriginalRole = originalRole,
            CustomerId = !string.IsNullOrEmpty(customerId) && int.TryParse(customerId, out var cid) ? cid : null,
            CustomerName = customerName
        });
    }
}
