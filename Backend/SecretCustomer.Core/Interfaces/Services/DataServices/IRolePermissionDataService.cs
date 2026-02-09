using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IRolePermissionDataService
{
    Task<RolePermission?> GetByIdAsync(int id);
    Task<List<RolePermission>> GetAllAsync();
    Task AddAsync(RolePermission entity);
    Task UpdateAsync(RolePermission entity);
    Task DeleteAsync(int id);
}
