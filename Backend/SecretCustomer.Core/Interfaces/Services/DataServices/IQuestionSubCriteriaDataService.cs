using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IQuestionSubCriteriaDataService
{
    Task<QuestionSubCriteria?> GetByIdAsync(int id);
    Task<List<QuestionSubCriteria>> GetAllAsync();
    Task AddAsync(QuestionSubCriteria entity);
    Task UpdateAsync(QuestionSubCriteria entity);
    Task DeleteAsync(int id);
}
