using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ITrainingVideoParticipantDataService
{
    Task<TrainingVideoParticipant?> GetByIdAsync(int id);
    Task<List<TrainingVideoParticipant>> GetAllAsync();
    Task AddAsync(TrainingVideoParticipant entity);
    Task UpdateAsync(TrainingVideoParticipant entity);
    Task DeleteAsync(int id);
}
