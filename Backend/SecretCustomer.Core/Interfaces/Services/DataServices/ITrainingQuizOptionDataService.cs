using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ITrainingQuizOptionDataService
{
    Task<TrainingQuizOption?> GetByIdAsync(int id);
    Task<List<TrainingQuizOption>> GetAllAsync();
    Task AddAsync(TrainingQuizOption entity);
    Task UpdateAsync(TrainingQuizOption entity);
    Task DeleteAsync(int id);
}
