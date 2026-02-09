using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IEvaluationDataService
{
    Task<Evaluation?> GetByIdAsync(int id);
    Task<List<Evaluation>> GetAllAsync();
    Task AddAsync(Evaluation entity);
    Task UpdateAsync(Evaluation entity);
    Task DeleteAsync(int id);
}
