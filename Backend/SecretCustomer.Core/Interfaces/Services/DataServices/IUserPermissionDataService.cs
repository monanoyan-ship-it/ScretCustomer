using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IUserPermissionDataService
{
    Task<UserPermission?> GetByIdAsync(int id);
    Task<List<UserPermission>> GetAllAsync();
    Task AddAsync(UserPermission entity);
    Task UpdateAsync(UserPermission entity);
    Task DeleteAsync(int id);
}
