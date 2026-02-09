using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ITrainingQuizAnswerDataService
{
    Task<TrainingQuizAnswer?> GetByIdAsync(int id);
    Task<List<TrainingQuizAnswer>> GetAllAsync();
    Task AddAsync(TrainingQuizAnswer entity);
    Task UpdateAsync(TrainingQuizAnswer entity);
    Task DeleteAsync(int id);
}
