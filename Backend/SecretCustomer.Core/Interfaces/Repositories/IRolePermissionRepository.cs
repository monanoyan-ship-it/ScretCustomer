using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IRolePermissionRepository
{
    Task<RolePermission?> GetByIdAsync(Guid id);
    Task<IEnumerable<RolePermission>> GetByRoleAsync(UserRole role);
    Task<RolePermission?> GetByRoleAndPermissionAsync(UserRole role, Guid permissionId);
    Task<RolePermission> AddAsync(RolePermission rolePermission);
    Task UpdateAsync(RolePermission rolePermission);
    Task DeleteAsync(Guid id);
    Task DeleteByRoleAndPermissionAsync(UserRole role, Guid permissionId);
}
