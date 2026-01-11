using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IRolePermissionRepository
{
    Task<RolePermission?> GetByIdAsync(int id);
    Task<IEnumerable<RolePermission>> GetByRoleIdAsync(int roleId);
    Task<RolePermission?> GetByRoleIdAndPermissionAsync(int roleId, int permissionId);
    Task<RolePermission> AddAsync(RolePermission rolePermission);
    Task UpdateAsync(RolePermission rolePermission);
    Task DeleteAsync(int id);
    Task DeleteByRoleIdAndPermissionAsync(int roleId, int permissionId);
}
