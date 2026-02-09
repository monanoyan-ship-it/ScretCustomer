using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ITrainingVideoExternalEmailLogDataService
{
    Task<TrainingVideoExternalEmailLog?> GetByIdAsync(int id);
    Task<List<TrainingVideoExternalEmailLog>> GetAllAsync();
    Task AddAsync(TrainingVideoExternalEmailLog entity);
    Task UpdateAsync(TrainingVideoExternalEmailLog entity);
    Task DeleteAsync(int id);
}
