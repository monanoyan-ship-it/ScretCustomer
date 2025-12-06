using SecretCustomer.Core.DTOs.Customer;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ICustomerPersonnelService
{
    Task<CustomerPersonnelDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<CustomerPersonnelDto>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<CustomerPersonnelDto>> GetByCustomerIdAsync(Guid customerId, bool includeInactive = false);
    Task<CustomerPersonnelDto?> GetByUsernameAsync(string username);
    Task<CustomerPersonnelDto> CreateAsync(CreateCustomerPersonnelDto createDto);
    Task<CustomerPersonnelDto> UpdateAsync(Guid id, UpdateCustomerPersonnelDto updateDto);
    Task DeleteAsync(Guid id);
}
