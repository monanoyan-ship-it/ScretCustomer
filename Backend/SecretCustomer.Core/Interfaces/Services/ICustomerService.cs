using SecretCustomer.Core.DTOs.Customer;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ICustomerService
{
    Task<CustomerDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<CustomerDto>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<CustomerDto>> GetActiveAsync();
    Task<CustomerDto?> GetByTaxNumberAsync(string taxNumber);
    Task<CustomerDto> CreateAsync(CreateCustomerDto createCustomerDto);
    Task<CustomerDto> UpdateAsync(Guid id, UpdateCustomerDto updateCustomerDto);
    Task DeleteAsync(Guid id);
}
