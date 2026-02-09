using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ICustomerNotificationRuleDataService
{
    Task<CustomerNotificationRule?> GetByIdAsync(int id);
    Task<List<CustomerNotificationRule>> GetAllAsync();
    Task AddAsync(CustomerNotificationRule entity);
    Task UpdateAsync(CustomerNotificationRule entity);
    Task DeleteAsync(int id);
}
