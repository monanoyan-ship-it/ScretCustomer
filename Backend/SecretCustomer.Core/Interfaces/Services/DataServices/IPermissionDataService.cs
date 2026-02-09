using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IPermissionDataService
{
    Task<Permission?> GetByIdAsync(int id);
    Task<List<Permission>> GetAllAsync();
    Task AddAsync(Permission entity);
    Task UpdateAsync(Permission entity);
    Task DeleteAsync(int id);
}
