using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ITrainingVideoScopeDataService
{
    Task<TrainingVideoScope?> GetByIdAsync(int id);
    Task<List<TrainingVideoScope>> GetAllAsync();
    Task AddAsync(TrainingVideoScope entity);
    Task UpdateAsync(TrainingVideoScope entity);
    Task DeleteAsync(int id);
}
