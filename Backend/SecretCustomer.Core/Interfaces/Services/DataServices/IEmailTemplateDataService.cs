using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IEmailTemplateDataService
{
    Task<EmailTemplate?> GetByIdAsync(int id);
    Task<List<EmailTemplate>> GetAllAsync();
    Task AddAsync(EmailTemplate entity);
    Task UpdateAsync(EmailTemplate entity);
    Task DeleteAsync(int id);
}
