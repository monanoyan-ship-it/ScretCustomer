using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class SmtpProfileService : ISmtpProfileService
{
    private readonly ApplicationDbContext _context;

    public SmtpProfileService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<object>> GetAllAsync()
    {
        var profiles = await _context.SmtpProfiles
            .OrderByDescending(p => p.IsDefault)
            .ThenBy(p => p.Name)
            .Select(p => new
            {
                p.Id,
                p.Name,
                p.Host,
                p.Port,
                p.Username,
                p.Password,
                p.UseSsl,
                p.FromEmail,
                p.FromName,
                p.Enabled,
                p.IsDefault,
                p.UseOAuth,
                p.TenantId,
                p.ClientId,
                p.ClientSecret,
                p.UseGraphApi,
                p.CreatedAt
            })
            .ToListAsync();

        return profiles.Cast<object>().ToList();
    }

    public async Task<object?> GetByIdAsync(int id)
    {
        var profile = await _context.SmtpProfiles.FindAsync(id);
        if (profile == null)
            return null;

        return new
        {
            profile.Id,
            profile.Name,
            profile.Host,
            profile.Port,
            profile.Username,
            profile.Password,
            profile.UseSsl,
            profile.FromEmail,
            profile.FromName,
            profile.Enabled,
            profile.IsDefault,
            profile.UseOAuth,
            profile.TenantId,
            profile.ClientId,
            profile.ClientSecret,
            profile.UseGraphApi,
            profile.CreatedAt
        };
    }

    public async Task<(bool Success, int Id, string Message)> CreateAsync(SmtpProfile profile, bool isDefault)
    {
        if (string.IsNullOrWhiteSpace(profile.Name))
            return (false, 0, "Profil adı gerekli.");

        if (string.IsNullOrWhiteSpace(profile.Host))
            return (false, 0, "SMTP sunucu adresi gerekli.");

        if (string.IsNullOrWhiteSpace(profile.FromEmail))
            return (false, 0, "Gönderen email adresi gerekli.");

        // Eğer bu profil default yapılıyorsa, diğerlerini false yap
        if (isDefault)
        {
            await ClearDefaultFlagAsync();
            profile.IsDefault = true;
        }

        // Eğer hiç profil yoksa, ilk profili otomatik default yap
        var hasAnyProfile = await _context.SmtpProfiles.AnyAsync();
        if (!hasAnyProfile)
        {
            profile.IsDefault = true;
        }

        _context.SmtpProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return (true, profile.Id, "Profil oluşturuldu.");
    }

    public async Task<(bool Success, string Message)> UpdateAsync(int id, SmtpProfile updatedData, bool isDefault)
    {
        var profile = await _context.SmtpProfiles.FindAsync(id);
        if (profile == null)
            return (false, "Profil bulunamadı.");

        if (string.IsNullOrWhiteSpace(updatedData.Name))
            return (false, "Profil adı gerekli.");

        if (string.IsNullOrWhiteSpace(updatedData.Host))
            return (false, "SMTP sunucu adresi gerekli.");

        if (string.IsNullOrWhiteSpace(updatedData.FromEmail))
            return (false, "Gönderen email adresi gerekli.");

        profile.Name = updatedData.Name.Trim();
        profile.Host = updatedData.Host.Trim();
        profile.Port = updatedData.Port;
        profile.Username = updatedData.Username?.Trim();
        profile.Password = updatedData.Password;
        profile.UseSsl = updatedData.UseSsl;
        profile.FromEmail = updatedData.FromEmail.Trim();
        profile.FromName = updatedData.FromName?.Trim();
        profile.Enabled = updatedData.Enabled;
        profile.UseOAuth = updatedData.UseOAuth;
        profile.TenantId = updatedData.TenantId?.Trim();
        profile.ClientId = updatedData.ClientId?.Trim();
        profile.ClientSecret = updatedData.ClientSecret;
        profile.UseGraphApi = updatedData.UseGraphApi;

        // IsDefault güncelleme
        if (isDefault && !profile.IsDefault)
        {
            await ClearDefaultFlagAsync();
            profile.IsDefault = true;
        }

        await _context.SaveChangesAsync();

        return (true, "Profil güncellendi.");
    }

    public async Task<(bool Success, string Message)> DeleteAsync(int id)
    {
        var profile = await _context.SmtpProfiles.FindAsync(id);
        if (profile == null)
            return (false, "Profil bulunamadı.");

        if (profile.IsDefault)
            return (false, "Varsayılan profil silinemez. Önce başka bir profili varsayılan yapın.");

        profile.IsDeleted = true;
        await _context.SaveChangesAsync();

        return (true, "Profil silindi.");
    }

    public async Task<(bool Success, string Message)> SetDefaultAsync(int id)
    {
        var profile = await _context.SmtpProfiles.FindAsync(id);
        if (profile == null)
            return (false, "Profil bulunamadı.");

        await ClearDefaultFlagAsync();
        profile.IsDefault = true;
        await _context.SaveChangesAsync();

        return (true, $"'{profile.Name}' varsayılan profil yapıldı.");
    }

    public async Task<SmtpProfile?> FindByIdAsync(int id)
    {
        return await _context.SmtpProfiles.FindAsync(id);
    }

    /// <summary>
    /// Tüm profillerin IsDefault'ını false yapar
    /// </summary>
    private async Task ClearDefaultFlagAsync()
    {
        var defaultProfiles = await _context.SmtpProfiles
            .Where(p => p.IsDefault)
            .ToListAsync();

        foreach (var p in defaultProfiles)
        {
            p.IsDefault = false;
        }
    }
}
