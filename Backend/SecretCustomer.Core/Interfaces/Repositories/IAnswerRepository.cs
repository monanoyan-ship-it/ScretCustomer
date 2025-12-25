using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IAnswerRepository
{
    Task<Answer?> GetByIdAsync(int id);
    Task<IEnumerable<Answer>> GetByEvaluationIdAsync(int evaluationId);
    Task<Answer> CreateAsync(Answer answer);
    Task<Answer> UpdateAsync(Answer answer);
    Task<bool> DeleteAsync(int id);
    Task<IEnumerable<Answer>> GetByQuestionIdAsync(int questionId);
}
