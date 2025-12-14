using SecretCustomer.Core.DTOs.Customer;
using SecretCustomer.Core.Entities;

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
    Task ChangePasswordAsync(Guid personnelId, CustomerPersonnelChangePasswordDto changePasswordDto);
    Task ResetPasswordAsync(Guid personnelId, AdminResetPasswordDto resetPasswordDto);

    // Customer Portal Login
    Task<CustomerPersonnel?> AuthenticateAsync(string username, string password);
}
