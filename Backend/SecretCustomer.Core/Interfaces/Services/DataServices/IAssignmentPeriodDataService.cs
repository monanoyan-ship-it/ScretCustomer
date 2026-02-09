using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IAssignmentPeriodDataService
{
    Task<AssignmentPeriod?> GetByIdAsync(int id);
    Task<List<AssignmentPeriod>> GetAllAsync();
    Task AddAsync(AssignmentPeriod entity);
    Task UpdateAsync(AssignmentPeriod entity);
    Task DeleteAsync(int id);
}
