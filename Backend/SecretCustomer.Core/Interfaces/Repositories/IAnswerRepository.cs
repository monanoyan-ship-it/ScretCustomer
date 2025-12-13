using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IAnswerRepository
{
    Task<Answer?> GetByIdAsync(Guid id);
    Task<IEnumerable<Answer>> GetByEvaluationIdAsync(Guid evaluationId);
    Task<Answer> CreateAsync(Answer answer);
    Task<Answer> UpdateAsync(Answer answer);
    Task<bool> DeleteAsync(Guid id);
    Task<IEnumerable<Answer>> GetByQuestionIdAsync(Guid questionId);
}
