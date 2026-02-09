using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IProjectFileDataService
{
    Task<ProjectFile?> GetByIdAsync(int id);
    Task<List<ProjectFile>> GetAllAsync();
    Task AddAsync(ProjectFile entity);
    Task UpdateAsync(ProjectFile entity);
    Task DeleteAsync(int id);
}
