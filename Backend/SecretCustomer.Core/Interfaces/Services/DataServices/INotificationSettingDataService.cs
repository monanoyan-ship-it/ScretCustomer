using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface INotificationSettingDataService
{
    Task<NotificationSetting?> GetByIdAsync(int id);
    Task<List<NotificationSetting>> GetAllAsync();
    Task AddAsync(NotificationSetting entity);
    Task UpdateAsync(NotificationSetting entity);
    Task DeleteAsync(int id);
}
