using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ICustomerPersonnelTaskAssignmentDataService
{
    Task<CustomerPersonnelTaskAssignment?> GetByIdAsync(int id);
    Task<List<CustomerPersonnelTaskAssignment>> GetAllAsync();
    Task AddAsync(CustomerPersonnelTaskAssignment entity);
    Task UpdateAsync(CustomerPersonnelTaskAssignment entity);
    Task DeleteAsync(int id);
}
