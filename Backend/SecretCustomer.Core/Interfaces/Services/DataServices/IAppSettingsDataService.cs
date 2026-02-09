using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IAppSettingsDataService
{
    Task<AppSettings?> GetByIdAsync(int id);
    Task<List<AppSettings>> GetAllAsync();
    Task AddAsync(AppSettings entity);
    Task UpdateAsync(AppSettings entity);
    Task DeleteAsync(int id);
}
