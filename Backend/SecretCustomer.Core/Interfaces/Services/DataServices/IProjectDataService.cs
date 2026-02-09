using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IProjectDataService
{
    Task<Project?> GetByIdAsync(int id);
    Task<List<Project>> GetAllAsync();
    Task AddAsync(Project entity);
    Task UpdateAsync(Project entity);
    Task DeleteAsync(int id);
}
