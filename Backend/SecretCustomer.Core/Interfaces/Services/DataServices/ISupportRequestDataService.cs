using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ISupportRequestDataService
{
    Task<SupportRequest?> GetByIdAsync(int id);
    Task<List<SupportRequest>> GetAllAsync();
    Task AddAsync(SupportRequest entity);
    Task UpdateAsync(SupportRequest entity);
    Task DeleteAsync(int id);
}
