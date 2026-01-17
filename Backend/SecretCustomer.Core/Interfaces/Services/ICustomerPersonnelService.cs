using SecretCustomer.Core.DTOs.Customer;
using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services;

public interface ICustomerPersonnelService
{
    Task<CustomerPersonnelDto?> GetByIdAsync(int id);
    Task<IEnumerable<CustomerPersonnelDto>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<CustomerPersonnelDto>> GetByCustomerIdAsync(int customerId, bool includeInactive = false);

    /// <summary>
    /// Server-side filtreleme ile personel listesi - sayfalama ve çoklu filtre desteği
    /// </summary>
    Task<PagedPersonnelResult> GetFilteredListAsync(PersonnelFilterDto filter);
    Task<IEnumerable<CustomerPersonnelDto>> GetByOrganizationIdAsync(int organizationId, bool includeInactive = false);
    Task ChangeOrganizationAsync(int personnelId, int newOrganizationId);
    Task<CustomerPersonnelDto?> GetByUsernameAsync(string username);
    Task<CustomerPersonnelDto> CreateAsync(CreateCustomerPersonnelDto createDto);
    Task<CustomerPersonnelDto> UpdateAsync(int id, UpdateCustomerPersonnelDto updateDto);
    Task DeleteAsync(int id);
    Task ChangePasswordAsync(int personnelId, CustomerPersonnelChangePasswordDto changePasswordDto);
    Task ResetPasswordAsync(int personnelId, AdminResetPasswordDto resetPasswordDto);

    // Customer Portal Login
    Task<CustomerPersonnel?> AuthenticateAsync(string username, string password);

    // Uniqueness checks
    Task<bool> ExistsByUsernameAsync(string username, int? excludeId = null);
    Task<bool> ExistsByEmailAsync(string email, int? excludeId = null);
}
