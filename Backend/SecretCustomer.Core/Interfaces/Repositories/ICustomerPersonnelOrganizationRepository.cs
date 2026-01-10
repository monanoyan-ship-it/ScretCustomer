using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface ICustomerPersonnelOrganizationRepository
{
    Task<CustomerPersonnelOrganization?> GetByIdAsync(int id);
    Task<CustomerPersonnelOrganization?> GetAsync(int personnelId, int organizationId);
    Task<IEnumerable<CustomerPersonnelOrganization>> GetByPersonnelIdAsync(int personnelId);
    Task<IEnumerable<CustomerPersonnelOrganization>> GetByOrganizationIdAsync(int organizationId);
    Task<CustomerPersonnelOrganization> AddAsync(CustomerPersonnelOrganization assignment);
    Task UpdateAsync(CustomerPersonnelOrganization assignment);
    Task DeleteAsync(int id);
    Task DeleteAsync(int personnelId, int organizationId);
    Task DeleteAsync(int personnelId, int organizationId, int? supervisorId);
    Task<bool> ExistsAsync(int personnelId, int organizationId);
}
