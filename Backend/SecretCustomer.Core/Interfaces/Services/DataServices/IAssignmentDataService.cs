using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IAssignmentDataService
{
    Task<Assignment?> GetByIdAsync(int id);
    Task<List<Assignment>> GetAllAsync();
    Task AddAsync(Assignment entity);
    Task UpdateAsync(Assignment entity);
    Task DeleteAsync(int id);
}
