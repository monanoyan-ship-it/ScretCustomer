using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ITrainingQuizQuestionDataService
{
    Task<TrainingQuizQuestion?> GetByIdAsync(int id);
    Task<List<TrainingQuizQuestion>> GetAllAsync();
    Task AddAsync(TrainingQuizQuestion entity);
    Task UpdateAsync(TrainingQuizQuestion entity);
    Task DeleteAsync(int id);
}
