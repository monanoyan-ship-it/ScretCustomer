using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IUserPermissionRepository
{
    Task<UserPermission?> GetByIdAsync(int id);
    Task<IEnumerable<UserPermission>> GetByUserIdAsync(int userId);
    Task<UserPermission?> GetByUserAndPermissionAsync(int userId, int permissionId);
    Task<UserPermission> AddAsync(UserPermission userPermission);
    Task UpdateAsync(UserPermission userPermission);
    Task DeleteAsync(int id);
    Task DeleteByUserAndPermissionAsync(int userId, int permissionId);
}
