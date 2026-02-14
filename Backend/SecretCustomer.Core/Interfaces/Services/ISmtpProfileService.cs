using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ISmtpProfileService
{
    Task<List<object>> GetAllAsync();
    Task<object?> GetByIdAsync(int id);
    Task<(bool Success, int Id, string Message)> CreateAsync(SmtpProfile profile, bool isDefault);
    Task<(bool Success, string Message)> UpdateAsync(int id, SmtpProfile updatedData, bool isDefault);
    Task<(bool Success, string Message)> DeleteAsync(int id);
    Task<(bool Success, string Message)> SetDefaultAsync(int id);
    Task<SmtpProfile?> FindByIdAsync(int id);
}
