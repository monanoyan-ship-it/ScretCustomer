using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IChecklistDataService
{
    Task<Checklist?> GetByIdAsync(int id);
    Task<List<Checklist>> GetAllAsync();
    Task AddAsync(Checklist entity);
    Task UpdateAsync(Checklist entity);
    Task DeleteAsync(int id);
}
