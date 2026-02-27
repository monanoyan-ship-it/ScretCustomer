using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SecretCustomer.Core.DTOs.Auth;
using SecretCustomer.Core.Interfaces.Services;
using System.Security.Claims;

namespace SecretCustomer.API.Controllers.Api;

/// <summary>
/// CustomerPersonnel için profil yönetimi
/// </summary>
[ApiController]
[Route("api/customer-portal/profile")]
[Authorize]
public class CustomerPortalProfileController : BaseApiController
{
    private readonly ICustomerPortalProfileService _profileService;
    private readonly IAuditLogService _auditLogService;
    private readonly ILocalizationService _localizationService;

    public CustomerPortalProfileController(
        ICustomerPortalProfileService profileService,
        IAuditLogService auditLogService,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _profileService = profileService;
        _auditLogService = auditLogService;
        _localizationService = localizationService;
    }

    private int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.Parse(userIdClaim!);
    }

    private string? GetUserType()
    {
        return User.FindFirst("UserType")?.Value;
    }

    /// <summary>
    /// Mevcut CustomerPersonnel'in profil bilgilerini getirir
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            // Sadece CustomerPersonnel erişebilir
            if (GetUserType() != "CustomerPersonnel")
            {
                return Forbid();
            }

            var userId = GetCurrentUserId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            var canManageCompanySettings = role == "CustomerManager" || role == "CustomerAdmin";

            var result = await _profileService.GetProfileAsync(userId, role, canManageCompanySettings);

            if (result == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Profile.NotFound")));
            }

            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting customer personnel profile", "CustomerPortalProfile", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Profile.LoadError"), ex));
        }
    }

    /// <summary>
    /// Mevcut CustomerPersonnel'in profil bilgilerini günceller
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> UpdateProfile([FromBody] CustomerPersonnelProfileUpdateDto dto)
    {
        try
        {
            if (GetUserType() != "CustomerPersonnel")
            {
                return Forbid();
            }

            var userId = GetCurrentUserId();
            var (success, result, errorMessage) = await _profileService.UpdateProfileAsync(userId, dto.FirstName, dto.LastName, dto.Email, dto.PhoneNumber);

            if (!success && result == null)
            {
                if (errorMessage == await _localizationService.GetResourceAsync("Api.Profile.EmailExists", defaultValue: "Bu e-posta adresi zaten kullanılıyor."))
                    return BadRequest(CreateErrorResponse(errorMessage!));

                return NotFound(CreateErrorResponse(errorMessage!));
            }

            await _auditLogService.LogInfoAsync($"CustomerPersonnel {userId} updated their profile", "CustomerPortalProfile");

            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating customer personnel profile", "CustomerPortalProfile", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Profile.UpdateError"), ex));
        }
    }

    /// <summary>
    /// Mevcut CustomerPersonnel'in şifresini değiştirir
    /// </summary>
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        try
        {
            if (GetUserType() != "CustomerPersonnel")
            {
                return Forbid();
            }

            var userId = GetCurrentUserId();
            var (success, message) = await _profileService.ChangePasswordAsync(userId, dto.CurrentPassword, dto.NewPassword, dto.ConfirmPassword);

            if (!success)
            {
                return BadRequest(CreateErrorResponse(message));
            }

            await _auditLogService.LogInfoAsync($"CustomerPersonnel {userId} changed their password", "CustomerPortalProfile");

            return Ok(new { message });
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error changing customer personnel password", "CustomerPortalProfile", ex);
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Profile.PasswordChangeError"), ex));
        }
    }

    /// <summary>
    /// Firma ayarlarını getirir (sadece Manager ve Admin)
    /// </summary>
    [HttpGet("company-settings")]
    public async Task<IActionResult> GetCompanySettings()
    {
        try
        {
            // Admin "müşteri gözünden" modu
            var adminCustomerId = HttpContext.Session.GetInt32("AdminViewAsCustomerId");
            if (adminCustomerId.HasValue && User.IsInRole("Admin"))
            {
                var result = await _profileService.GetCompanySettingsAsync(adminCustomerId, null);
                if (result == null)
                    return NotFound(CreateErrorResponse("Firma bulunamadı"));

                return Ok(result);
            }

            if (GetUserType() != "CustomerPersonnel")
            {
                return Forbid();
            }

            var userId = GetCurrentUserId();
            var settings = await _profileService.GetCompanySettingsAsync(null, userId);

            if (settings == null)
            {
                return NotFound(CreateErrorResponse("Firma bulunamadı"));
            }

            return Ok(settings);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error getting company settings", "CustomerPortalProfile", ex);
            return StatusCode(500, CreateErrorResponse("Firma ayarları yüklenirken bir hata oluştu", ex));
        }
    }

    /// <summary>
    /// Firma ayarlarını günceller (sadece Manager ve Admin)
    /// </summary>
    [HttpPut("company-settings")]
    public async Task<IActionResult> UpdateCompanySettings([FromBody] CompanySettingsUpdateDto dto)
    {
        try
        {
            if (GetUserType() != "CustomerPersonnel")
            {
                return Forbid();
            }

            // Sadece Manager ve Admin erişebilir
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "CustomerManager" && role != "CustomerAdmin")
            {
                return Forbid();
            }

            var userId = GetCurrentUserId();
            var (success, result, errorMessage) = await _profileService.UpdateCompanySettingsAsync(userId, dto.CallSystemUrl);

            if (!success)
            {
                return NotFound(CreateErrorResponse(errorMessage!));
            }

            await _auditLogService.LogInfoAsync($"Company settings updated by PersonnelId {userId}", "CustomerPortalProfile");

            return Ok(result);
        }
        catch (Exception ex)
        {
            await _auditLogService.LogErrorAsync($"Error updating company settings", "CustomerPortalProfile", ex);
            return StatusCode(500, CreateErrorResponse("Firma ayarları güncellenirken bir hata oluştu", ex));
        }
    }
}

/// <summary>
/// CustomerPersonnel profil güncelleme DTO
/// </summary>
public class CustomerPersonnelProfileUpdateDto
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
}

/// <summary>
/// Firma ayarları güncelleme DTO
/// </summary>
public class CompanySettingsUpdateDto
{
    /// <summary>
    /// Çağrı sistemi URL şablonu. {CallId} placeholder ile çağrı detay sayfasına yönlendirme yapılır.
    /// Örnek: https://firmasistemi.com/calls/{CallId}
    /// </summary>
    public string? CallSystemUrl { get; set; }
}
