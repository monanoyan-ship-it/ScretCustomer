using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IAssignmentRepository
{
    Task<Assignment?> GetByIdAsync(Guid id, bool includeDetails = false);
    Task<IEnumerable<Assignment>> GetAllAsync();
    Task<Assignment?> GetByUniqueLinkAsync(string uniqueLink, bool includeDetails = false);
    Task<IEnumerable<Assignment>> GetByProjectIdAsync(Guid projectId);
    Task<IEnumerable<Assignment>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<Assignment>> GetByBranchIdAsync(Guid branchId);
    Task<Assignment> CreateAsync(Assignment assignment);
    Task<IEnumerable<Assignment>> CreateBulkAsync(IEnumerable<Assignment> assignments);
    Task<Assignment> UpdateAsync(Assignment assignment);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsByEmailAsync(Guid projectId, string email);
}
