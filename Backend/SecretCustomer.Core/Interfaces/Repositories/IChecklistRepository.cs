using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IChecklistRepository
{
    Task<Checklist?> GetByIdAsync(int id, bool includeDetails = false);
    Task<IEnumerable<Checklist>> GetAllAsync(bool includeInactive = false);
    Task<Checklist> CreateAsync(Checklist checklist);
    Task<Checklist> UpdateAsync(Checklist checklist);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsAsync(int id);
    Task<int> GetVersionCountAsync(string name);
}
