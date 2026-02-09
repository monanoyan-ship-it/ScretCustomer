using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IPerformanceSettingsDataService
{
    Task<PerformanceSettings?> GetByIdAsync(int id);
    Task<List<PerformanceSettings>> GetAllAsync();
    Task AddAsync(PerformanceSettings entity);
    Task UpdateAsync(PerformanceSettings entity);
    Task DeleteAsync(int id);
}
