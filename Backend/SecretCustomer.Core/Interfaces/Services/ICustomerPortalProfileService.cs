namespace SecretCustomer.Core.Interfaces.Services;

public interface ICustomerPortalProfileService
{
    Task<object?> GetProfileAsync(int personnelId, string? role, bool canManageCompanySettings);
    Task<(bool Success, object? Result, string? ErrorMessage)> UpdateProfileAsync(int personnelId, string? firstName, string? lastName, string? email, string? phoneNumber);
    Task<(bool Success, string Message)> ChangePasswordAsync(int personnelId, string currentPassword, string newPassword, string confirmPassword);
    Task<object?> GetCompanySettingsAsync(int? adminCustomerId, int? personnelId);
    Task<(bool Success, object? Result, string? ErrorMessage)> UpdateCompanySettingsAsync(int personnelId, string? callSystemUrl);
}
