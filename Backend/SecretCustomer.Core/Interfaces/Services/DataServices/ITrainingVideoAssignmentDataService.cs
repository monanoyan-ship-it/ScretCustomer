using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ITrainingVideoAssignmentDataService
{
    Task<TrainingVideoAssignment?> GetByIdAsync(int id);
    Task<List<TrainingVideoAssignment>> GetAllAsync();
    Task AddAsync(TrainingVideoAssignment entity);
    Task UpdateAsync(TrainingVideoAssignment entity);
    Task DeleteAsync(int id);
}
