using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ITrainingQuizDataService
{
    Task<TrainingQuiz?> GetByIdAsync(int id);
    Task<List<TrainingQuiz>> GetAllAsync();
    Task AddAsync(TrainingQuiz entity);
    Task UpdateAsync(TrainingQuiz entity);
    Task DeleteAsync(int id);
}
