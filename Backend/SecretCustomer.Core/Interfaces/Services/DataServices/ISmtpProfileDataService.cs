using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ISmtpProfileDataService
{
    Task<SmtpProfile?> GetByIdAsync(int id);
    Task<List<SmtpProfile>> GetAllAsync();
    Task AddAsync(SmtpProfile entity);
    Task UpdateAsync(SmtpProfile entity);
    Task DeleteAsync(int id);
}
