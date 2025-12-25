using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IBranchRepository
{
    Task<Branch?> GetByIdAsync(int id, bool includeDetails = false);
    Task<IEnumerable<Branch>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<Branch>> GetActiveAsync();
    Task<Branch?> GetByCodeAsync(string code);
    Task<IEnumerable<Branch>> GetByRegionAsync(string region);
    Task<IEnumerable<Branch>> GetByCityAsync(string city);
    Task<Branch> CreateAsync(Branch branch);
    Task<Branch> UpdateAsync(Branch branch);
    Task DeleteAsync(int id);
    Task<bool> ExistsByCodeAsync(string code, int? excludeId = null);
}
