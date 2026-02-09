using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ICustomerPersonnelPermissionDataService
{
    Task<CustomerPersonnelPermission?> GetByIdAsync(int id);
    Task<List<CustomerPersonnelPermission>> GetAllAsync();
    Task AddAsync(CustomerPersonnelPermission entity);
    Task UpdateAsync(CustomerPersonnelPermission entity);
    Task DeleteAsync(int id);
}
