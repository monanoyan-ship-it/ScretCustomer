using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IAnswerSubCriteriaSelectionDataService
{
    Task<AnswerSubCriteriaSelection?> GetByIdAsync(int id);
    Task<List<AnswerSubCriteriaSelection>> GetAllAsync();
    Task AddAsync(AnswerSubCriteriaSelection entity);
    Task UpdateAsync(AnswerSubCriteriaSelection entity);
    Task DeleteAsync(int id);
}
