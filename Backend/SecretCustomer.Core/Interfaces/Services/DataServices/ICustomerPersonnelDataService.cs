using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ICustomerPersonnelDataService
{
    Task<CustomerPersonnel?> GetByIdAsync(int id);
    Task<List<CustomerPersonnel>> GetAllAsync();
    Task AddAsync(CustomerPersonnel entity);
    Task UpdateAsync(CustomerPersonnel entity);
    Task DeleteAsync(int id);
}
