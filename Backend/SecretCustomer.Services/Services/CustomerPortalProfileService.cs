using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class CustomerPortalProfileService : ICustomerPortalProfileService
{
    private readonly ApplicationDbContext _context;
    private readonly ILocalizationService _localizationService;

    public CustomerPortalProfileService(ApplicationDbContext context, ILocalizationService localizationService)
    {
        _context = context;
        _localizationService = localizationService;
    }

    public async Task<object?> GetProfileAsync(int personnelId, string? role, bool canManageCompanySettings)
    {
        var personnel = await _context.CustomerPersonnel
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personnelId && !p.IsDeleted);

        if (personnel == null)
            return null;

        return new
        {
            id = personnel.Id,
            username = personnel.Username,
            email = personnel.Email,
            firstName = personnel.FirstName,
            lastName = personnel.LastName,
            fullName = $"{personnel.FirstName} {personnel.LastName}",
            phoneNumber = personnel.PhoneNumber,
            department = personnel.Department,
            title = personnel.Title,
            role = role,
            canManageCompanySettings = canManageCompanySettings
        };
    }

    public async Task<(bool Success, object? Result, string? ErrorMessage)> UpdateProfileAsync(int personnelId, string? firstName, string? lastName, string? email, string? phoneNumber)
    {
        var personnel = await _context.CustomerPersonnel
            .FirstOrDefaultAsync(p => p.Id == personnelId && !p.IsDeleted);

        if (personnel == null)
            return (false, null, await _localizationService.GetResourceAsync("Api.Profile.NotFound"));

        // Ad Soyad güncelle
        if (!string.IsNullOrWhiteSpace(firstName))
            personnel.FirstName = firstName.Trim();

        if (!string.IsNullOrWhiteSpace(lastName))
            personnel.LastName = lastName.Trim();

        // Email güncelle (tekillik kontrolü ile)
        if (!string.IsNullOrEmpty(email) && email != personnel.Email)
        {
            var emailExists = await _context.CustomerPersonnel
                .AnyAsync(p => p.CustomerId == personnel.CustomerId &&
                               p.Email == email &&
                               p.Id != personnelId &&
                               !p.IsDeleted);

            if (emailExists)
            {
                return (false, null, await _localizationService.GetResourceAsync("Api.Profile.EmailExists", defaultValue: "Bu e-posta adresi zaten kullanılıyor."));
            }

            personnel.Email = email.Trim();
        }

        // Telefon güncelle
        if (phoneNumber != null)
            personnel.PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

        personnel.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        return (true, new
        {
            id = personnel.Id,
            username = personnel.Username,
            email = personnel.Email,
            firstName = personnel.FirstName,
            lastName = personnel.LastName,
            fullName = $"{personnel.FirstName} {personnel.LastName}",
            phoneNumber = personnel.PhoneNumber
        }, null);
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(int personnelId, string currentPassword, string newPassword, string confirmPassword)
    {
        var personnel = await _context.CustomerPersonnel
            .FirstOrDefaultAsync(p => p.Id == personnelId && !p.IsDeleted);

        if (personnel == null)
            return (false, await _localizationService.GetResourceAsync("Api.Profile.NotFound"));

        // Mevcut şifre kontrolü
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, personnel.PasswordHash))
        {
            return (false, await _localizationService.GetResourceAsync("Api.Profile.WrongPassword", defaultValue: "Mevcut şifre yanlış."));
        }

        // Yeni şifre validasyonu
        if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
        {
            return (false, await _localizationService.GetResourceAsync("Api.Profile.PasswordTooShort", defaultValue: "Şifre en az 6 karakter olmalıdır."));
        }

        if (newPassword != confirmPassword)
        {
            return (false, await _localizationService.GetResourceAsync("Profile.PasswordMismatch", defaultValue: "Şifreler eşleşmiyor."));
        }

        // Şifreyi güncelle
        personnel.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        personnel.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();

        return (true, await _localizationService.GetResourceAsync("Api.Profile.PasswordChanged", defaultValue: "Şifreniz başarıyla değiştirildi."));
    }

    public async Task<object?> GetCompanySettingsAsync(int? adminCustomerId, int? personnelId)
    {
        // Admin "müşteri gözünden" modu
        if (adminCustomerId.HasValue)
        {
            var adminCustomer = await _context.Customers
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == adminCustomerId.Value && !c.IsDeleted);

            if (adminCustomer == null)
                return null;

            return new
            {
                customerId = adminCustomer.Id,
                companyName = adminCustomer.CompanyName,
                callSystemUrl = adminCustomer.CallSystemUrl
            };
        }

        if (!personnelId.HasValue)
            return null;

        var personnel = await _context.CustomerPersonnel
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personnelId.Value && !p.IsDeleted);

        if (personnel == null)
            return null;

        var customer = await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == personnel.CustomerId && !c.IsDeleted);

        if (customer == null)
            return null;

        return new
        {
            customerId = customer.Id,
            companyName = customer.CompanyName,
            callSystemUrl = customer.CallSystemUrl
        };
    }

    public async Task<(bool Success, object? Result, string? ErrorMessage)> UpdateCompanySettingsAsync(int personnelId, string? callSystemUrl)
    {
        var personnel = await _context.CustomerPersonnel
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == personnelId && !p.IsDeleted);

        if (personnel == null)
            return (false, null, "Kullanıcı bulunamadı");

        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == personnel.CustomerId && !c.IsDeleted);

        if (customer == null)
            return (false, null, "Firma bulunamadı");

        // CallSystemUrl güncelle
        customer.CallSystemUrl = string.IsNullOrWhiteSpace(callSystemUrl) ? null : callSystemUrl.Trim();
        customer.UpdatedAt = TurkeyTime.Now;

        await _context.SaveChangesAsync();

        return (true, new
        {
            customerId = customer.Id,
            companyName = customer.CompanyName,
            callSystemUrl = customer.CallSystemUrl,
            message = "Firma ayarları başarıyla güncellendi"
        }, null);
    }
}
