using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IChecklistRepository
{
    Task<Checklist?> GetByIdAsync(Guid id, bool includeDetails = false);
    Task<IEnumerable<Checklist>> GetAllAsync(bool includeInactive = false);
    Task<Checklist> CreateAsync(Checklist checklist);
    Task<Checklist> UpdateAsync(Checklist checklist);
    Task<bool> DeleteAsync(Guid id);
    Task<bool> ExistsAsync(Guid id);
    Task<int> GetVersionCountAsync(string name);
}
