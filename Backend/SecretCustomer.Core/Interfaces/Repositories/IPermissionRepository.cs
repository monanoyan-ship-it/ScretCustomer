using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IPermissionRepository
{
    Task<Permission?> GetByIdAsync(Guid id);
    Task<Permission?> GetByCodeAsync(string code);
    Task<IEnumerable<Permission>> GetAllAsync();
    Task<IEnumerable<Permission>> GetByCategoryAsync(PermissionCategory category);
    Task<Permission> AddAsync(Permission permission);
    Task UpdateAsync(Permission permission);
    Task DeleteAsync(Guid id);
}
