using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IAssignmentRepository
{
    Task<Assignment?> GetByIdAsync(int id, bool includeDetails = false);
    Task<IEnumerable<Assignment>> GetAllAsync();
    Task<Assignment?> GetByUniqueLinkAsync(string uniqueLink, bool includeDetails = false);
    Task<IEnumerable<Assignment>> GetByProjectIdAsync(int projectId);
    Task<IEnumerable<Assignment>> GetByUserIdAsync(int userId);
    Task<IEnumerable<Assignment>> GetByFieldWorkerIdAsync(int fieldWorkerId);
    Task<Assignment> CreateAsync(Assignment assignment);
    Task<IEnumerable<Assignment>> CreateBulkAsync(IEnumerable<Assignment> assignments);
    Task<Assignment> UpdateAsync(Assignment assignment);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsByEmailAsync(int projectId, string email);
}
