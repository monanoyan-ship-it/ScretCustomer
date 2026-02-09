using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ICustomerPersonnelNotificationLogDataService
{
    Task<CustomerPersonnelNotificationLog?> GetByIdAsync(int id);
    Task<List<CustomerPersonnelNotificationLog>> GetAllAsync();
    Task AddAsync(CustomerPersonnelNotificationLog entity);
    Task UpdateAsync(CustomerPersonnelNotificationLog entity);
    Task DeleteAsync(int id);
}
