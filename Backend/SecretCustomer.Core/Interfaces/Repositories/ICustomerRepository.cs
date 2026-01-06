using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id, bool includeDetails = false);
    Task<IEnumerable<Customer>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<Customer>> GetActiveAsync();
    Task<Customer?> GetByTaxNumberAsync(string taxNumber);
    Task<Customer?> GetByEmailAsync(string email);
    Task<Customer?> GetByNameAsync(string companyName);
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
    Task<bool> ExistsByTaxNumberAsync(string taxNumber, int? excludeId = null);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
}
