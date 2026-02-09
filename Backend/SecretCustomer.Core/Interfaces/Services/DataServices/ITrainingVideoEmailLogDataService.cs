using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ITrainingVideoEmailLogDataService
{
    Task<TrainingVideoEmailLog?> GetByIdAsync(int id);
    Task<List<TrainingVideoEmailLog>> GetAllAsync();
    Task AddAsync(TrainingVideoEmailLog entity);
    Task UpdateAsync(TrainingVideoEmailLog entity);
    Task DeleteAsync(int id);
}
