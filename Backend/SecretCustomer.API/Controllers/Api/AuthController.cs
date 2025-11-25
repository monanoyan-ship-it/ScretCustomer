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

    public AuthController(
        IAuthService authService,
        JwtHelper jwtHelper,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _jwtHelper = jwtHelper;
        _logger = logger;
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
                return Unauthorized(new { message = "Geçersiz kullanıcı adı veya şifre." });
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
            return StatusCode(500, new { message = "Giriş sırasında bir hata oluştu." });
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
    public IActionResult Logout()
    {
        // JWT is stateless, so just return success
        // Client will delete the token
        _logger.LogInformation("User logged out");
        return Ok(new { message = "Logout successful" });
    }
}
