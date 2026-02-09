using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IAnswerDataService
{
    Task<Answer?> GetByIdAsync(int id);
    Task<List<Answer>> GetAllAsync();
    Task AddAsync(Answer entity);
    Task UpdateAsync(Answer entity);
    Task DeleteAsync(int id);
}
