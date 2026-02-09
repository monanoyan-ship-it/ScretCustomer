using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ICustomerTaskListDataService
{
    Task<CustomerTaskList?> GetByIdAsync(int id);
    Task<List<CustomerTaskList>> GetAllAsync();
    Task AddAsync(CustomerTaskList entity);
    Task UpdateAsync(CustomerTaskList entity);
    Task DeleteAsync(int id);
}
