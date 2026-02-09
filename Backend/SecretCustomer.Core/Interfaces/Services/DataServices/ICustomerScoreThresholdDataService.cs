using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ICustomerScoreThresholdDataService
{
    Task<CustomerScoreThreshold?> GetByIdAsync(int id);
    Task<List<CustomerScoreThreshold>> GetAllAsync();
    Task AddAsync(CustomerScoreThreshold entity);
    Task UpdateAsync(CustomerScoreThreshold entity);
    Task DeleteAsync(int id);
}
