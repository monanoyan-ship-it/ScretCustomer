using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ITrainingVideoDataService
{
    Task<TrainingVideo?> GetByIdAsync(int id);
    Task<List<TrainingVideo>> GetAllAsync();
    Task AddAsync(TrainingVideo entity);
    Task UpdateAsync(TrainingVideo entity);
    Task DeleteAsync(int id);
}
