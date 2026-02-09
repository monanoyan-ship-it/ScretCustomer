using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ITrainingQuizResponseDataService
{
    Task<TrainingQuizResponse?> GetByIdAsync(int id);
    Task<List<TrainingQuizResponse>> GetAllAsync();
    Task AddAsync(TrainingQuizResponse entity);
    Task UpdateAsync(TrainingQuizResponse entity);
    Task DeleteAsync(int id);
}
