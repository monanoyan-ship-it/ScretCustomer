using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ITrainingVideoExternalParticipantDataService
{
    Task<TrainingVideoExternalParticipant?> GetByIdAsync(int id);
    Task<List<TrainingVideoExternalParticipant>> GetAllAsync();
    Task AddAsync(TrainingVideoExternalParticipant entity);
    Task UpdateAsync(TrainingVideoExternalParticipant entity);
    Task DeleteAsync(int id);
}
