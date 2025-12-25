using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface ICustomerPersonnelRepository
{
    Task<CustomerPersonnel?> GetByIdAsync(int id, bool includeDetails = false);
    Task<IEnumerable<CustomerPersonnel>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<CustomerPersonnel>> GetByCustomerIdAsync(int customerId, bool includeInactive = false);
    Task<CustomerPersonnel?> GetByUsernameAsync(string username);
    Task<CustomerPersonnel?> GetByEmailAsync(string email);
    Task<CustomerPersonnel> CreateAsync(CustomerPersonnel personnel);
    Task<CustomerPersonnel> UpdateAsync(CustomerPersonnel personnel);
    Task DeleteAsync(int id);
    Task<bool> ExistsByUsernameAsync(string username, int? excludeId = null);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
}
