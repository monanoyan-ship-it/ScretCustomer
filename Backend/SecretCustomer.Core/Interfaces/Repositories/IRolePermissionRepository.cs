using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IRolePermissionRepository
{
    Task<RolePermission?> GetByIdAsync(int id);
    Task<IEnumerable<RolePermission>> GetByRoleAsync(UserRole role);
    Task<RolePermission?> GetByRoleAndPermissionAsync(UserRole role, int permissionId);
    Task<RolePermission> AddAsync(RolePermission rolePermission);
    Task UpdateAsync(RolePermission rolePermission);
    Task DeleteAsync(int id);
    Task DeleteByRoleAndPermissionAsync(UserRole role, int permissionId);
}
