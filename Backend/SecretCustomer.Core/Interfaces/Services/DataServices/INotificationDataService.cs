using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface INotificationDataService
{
    Task<Notification?> GetByIdAsync(int id);
    Task<List<Notification>> GetAllAsync();
    Task AddAsync(Notification entity);
    Task UpdateAsync(Notification entity);
    Task DeleteAsync(int id);
}
