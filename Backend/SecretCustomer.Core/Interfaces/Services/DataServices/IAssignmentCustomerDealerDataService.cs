using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IAssignmentCustomerDealerDataService
{
    Task<AssignmentCustomerDealer?> GetByIdAsync(int id);
    Task<List<AssignmentCustomerDealer>> GetAllAsync();
    Task AddAsync(AssignmentCustomerDealer entity);
    Task UpdateAsync(AssignmentCustomerDealer entity);
    Task DeleteAsync(int id);
}
