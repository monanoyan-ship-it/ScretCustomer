using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(Guid id, bool includeDetails = false);
    Task<IEnumerable<Customer>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<Customer>> GetActiveAsync();
    Task<Customer?> GetByTaxNumberAsync(string taxNumber);
    Task<Customer?> GetByEmailAsync(string email);
    Task<Customer> CreateAsync(Customer customer);
    Task<Customer> UpdateAsync(Customer customer);
    Task DeleteAsync(Guid id);
    Task<bool> ExistsByTaxNumberAsync(string taxNumber, Guid? excludeId = null);
    Task<bool> ExistsByEmailAsync(string email, Guid? excludeId = null);
}
