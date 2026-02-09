using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ICustomerOrganizationDataService
{
    Task<CustomerOrganization?> GetByIdAsync(int id);
    Task<List<CustomerOrganization>> GetAllAsync();
    Task AddAsync(CustomerOrganization entity);
    Task UpdateAsync(CustomerOrganization entity);
    Task DeleteAsync(int id);
}
