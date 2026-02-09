using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ISavedFilterDataService
{
    Task<SavedFilter?> GetByIdAsync(int id);
    Task<List<SavedFilter>> GetAllAsync();
    Task AddAsync(SavedFilter entity);
    Task UpdateAsync(SavedFilter entity);
    Task DeleteAsync(int id);
}
