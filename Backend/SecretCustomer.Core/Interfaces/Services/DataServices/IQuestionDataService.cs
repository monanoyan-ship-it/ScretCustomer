using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IQuestionDataService
{
    Task<Question?> GetByIdAsync(int id);
    Task<List<Question>> GetAllAsync();
    Task AddAsync(Question entity);
    Task UpdateAsync(Question entity);
    Task DeleteAsync(int id);
}
