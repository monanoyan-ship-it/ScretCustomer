using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ICustomerPersonnelOrganizationDataService
{
    Task<CustomerPersonnelOrganization?> GetByIdAsync(int id);
    Task<List<CustomerPersonnelOrganization>> GetAllAsync();
    Task AddAsync(CustomerPersonnelOrganization entity);
    Task UpdateAsync(CustomerPersonnelOrganization entity);
    Task DeleteAsync(int id);
}
