using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Auth;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

[ApiController]
[Route("api/profile")]
[Authorize]
public class ProfileApiController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ILogger<ProfileApiController> _logger;

    public ProfileApiController(IAuthService authService, ILogger<ProfileApiController> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim!);
    }

    /// <summary>
    /// Mevcut kullanıcının profil bilgilerini getirir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userId = GetCurrentUserId();
            var profile = await _authService.GetProfileAsync(userId);

            if (profile == null)
                return NotFound(new { message = "Profil bulunamadı." });

            return Ok(profile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting profile");
            return StatusCode(500, new { message = "Profil yüklenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Mevcut kullanıcının profil bilgilerini günceller
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var updatedProfile = await _authService.UpdateProfileAsync(userId, dto);

            if (updatedProfile == null)
                return NotFound(new { message = "Profil bulunamadı." });

            return Ok(updatedProfile);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating profile");
            return StatusCode(500, new { message = "Profil güncellenirken bir hata oluştu." });
        }
    }

    /// <summary>
    /// Mevcut kullanıcının şifresini değiştirir
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetCurrentUserId();
            var result = await _authService.ChangePasswordAsync(userId, dto);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return Ok(new { message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, new { message = "Şifre değiştirilirken bir hata oluştu." });
        }
    }
}
