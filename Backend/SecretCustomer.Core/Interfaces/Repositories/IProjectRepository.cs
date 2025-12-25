using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IProjectRepository
{
    Task<Project?> GetByIdAsync(int id, bool includeDetails = false);
    Task<IEnumerable<Project>> GetAllAsync(bool includeInactive = false);
    Task<Project> CreateAsync(Project project);
    Task<Project> UpdateAsync(Project project);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
}
