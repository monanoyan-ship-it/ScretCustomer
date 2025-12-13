using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IUserPermissionRepository
{
    Task<UserPermission?> GetByIdAsync(Guid id);
    Task<IEnumerable<UserPermission>> GetByUserIdAsync(Guid userId);
    Task<UserPermission?> GetByUserAndPermissionAsync(Guid userId, Guid permissionId);
    Task<UserPermission> AddAsync(UserPermission userPermission);
    Task UpdateAsync(UserPermission userPermission);
    Task DeleteAsync(Guid id);
    Task DeleteByUserAndPermissionAsync(Guid userId, Guid permissionId);
}
