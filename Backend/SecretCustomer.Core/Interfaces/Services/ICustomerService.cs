using SecretCustomer.Core.DTOs.Customer;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ICustomerService
{
    Task<CustomerDto?> GetByIdAsync(int id);
    Task<IEnumerable<CustomerDto>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<CustomerDto>> GetActiveAsync();
    Task<CustomerDto?> GetByTaxNumberAsync(string taxNumber);
    Task<CustomerDto> CreateAsync(CreateCustomerDto createCustomerDto);
    Task<CustomerDto> UpdateAsync(int id, UpdateCustomerDto updateCustomerDto);
    Task DeleteAsync(int id);
}
