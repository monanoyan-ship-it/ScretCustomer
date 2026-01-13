using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.Auth;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
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
    private readonly ApplicationDbContext _context;
    private readonly ILogger<CustomerPortalProfileController> _logger;
    private readonly ILocalizationService _localizationService;

    public CustomerPortalProfileController(
        ApplicationDbContext context,
        ILogger<CustomerPortalProfileController> logger,
        ILocalizationService localizationService,
        IConfiguration configuration) : base(configuration)
    {
        _context = context;
        _logger = logger;
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
            var personnel = await _context.CustomerPersonnel
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == userId && !p.IsDeleted);

            if (personnel == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Profile.NotFound")));
            }

            return Ok(new
            {
                id = personnel.Id,
                username = personnel.Username,
                email = personnel.Email,
                firstName = personnel.FirstName,
                lastName = personnel.LastName,
                fullName = $"{personnel.FirstName} {personnel.LastName}",
                phoneNumber = personnel.PhoneNumber,
                department = personnel.Department,
                title = personnel.Title
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer personnel profile");
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
            var personnel = await _context.CustomerPersonnel
                .FirstOrDefaultAsync(p => p.Id == userId && !p.IsDeleted);

            if (personnel == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Profile.NotFound")));
            }

            // Ad Soyad güncelle
            if (!string.IsNullOrWhiteSpace(dto.FirstName))
                personnel.FirstName = dto.FirstName.Trim();

            if (!string.IsNullOrWhiteSpace(dto.LastName))
                personnel.LastName = dto.LastName.Trim();

            // Email güncelle (tekillik kontrolü ile)
            if (!string.IsNullOrEmpty(dto.Email) && dto.Email != personnel.Email)
            {
                var emailExists = await _context.CustomerPersonnel
                    .AnyAsync(p => p.CustomerId == personnel.CustomerId &&
                                   p.Email == dto.Email &&
                                   p.Id != userId &&
                                   !p.IsDeleted);

                if (emailExists)
                {
                    return BadRequest(CreateErrorResponse(
                        await _localizationService.GetResourceAsync("Api.Profile.EmailExists", defaultValue: "Bu e-posta adresi zaten kullanılıyor.")));
                }

                personnel.Email = dto.Email.Trim();
            }

            // Telefon güncelle
            if (dto.PhoneNumber != null)
                personnel.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();

            personnel.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("CustomerPersonnel {Id} updated their profile", userId);

            return Ok(new
            {
                id = personnel.Id,
                username = personnel.Username,
                email = personnel.Email,
                firstName = personnel.FirstName,
                lastName = personnel.LastName,
                fullName = $"{personnel.FirstName} {personnel.LastName}",
                phoneNumber = personnel.PhoneNumber
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer personnel profile");
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
            var personnel = await _context.CustomerPersonnel
                .FirstOrDefaultAsync(p => p.Id == userId && !p.IsDeleted);

            if (personnel == null)
            {
                return NotFound(CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Profile.NotFound")));
            }

            // Mevcut şifre kontrolü
            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, personnel.PasswordHash))
            {
                return BadRequest(CreateErrorResponse(
                    await _localizationService.GetResourceAsync("Api.Profile.WrongPassword", defaultValue: "Mevcut şifre yanlış.")));
            }

            // Yeni şifre validasyonu
            if (string.IsNullOrEmpty(dto.NewPassword) || dto.NewPassword.Length < 6)
            {
                return BadRequest(CreateErrorResponse(
                    await _localizationService.GetResourceAsync("Api.Profile.PasswordTooShort", defaultValue: "Şifre en az 6 karakter olmalıdır.")));
            }

            if (dto.NewPassword != dto.ConfirmPassword)
            {
                return BadRequest(CreateErrorResponse(
                    await _localizationService.GetResourceAsync("Api.Profile.PasswordMismatch", defaultValue: "Şifreler eşleşmiyor.")));
            }

            // Şifreyi güncelle
            personnel.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            personnel.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            _logger.LogInformation("CustomerPersonnel {Id} changed their password", userId);

            return Ok(new { message = await _localizationService.GetResourceAsync("Api.Profile.PasswordChanged", defaultValue: "Şifreniz başarıyla değiştirildi.") });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing customer personnel password");
            return StatusCode(500, CreateErrorResponse(await _localizationService.GetResourceAsync("Api.Profile.PasswordChangeError"), ex));
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
