using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ICustomerDealerDataService
{
    Task<CustomerDealer?> GetByIdAsync(int id);
    Task<List<CustomerDealer>> GetAllAsync();
    Task AddAsync(CustomerDealer entity);
    Task UpdateAsync(CustomerDealer entity);
    Task DeleteAsync(int id);
}
