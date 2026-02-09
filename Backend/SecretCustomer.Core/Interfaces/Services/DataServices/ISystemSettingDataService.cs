using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ISystemSettingDataService
{
    Task<SystemSetting?> GetByIdAsync(int id);
    Task<List<SystemSetting>> GetAllAsync();
    Task AddAsync(SystemSetting entity);
    Task UpdateAsync(SystemSetting entity);
    Task DeleteAsync(int id);
}
