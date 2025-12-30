using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IEvaluationRepository
{
    Task<Evaluation?> GetByIdAsync(int id, bool includeDetails = false);
    Task<Evaluation?> GetByAssignmentIdAsync(int assignmentId, bool includeDetails = false);
    Task<IEnumerable<Evaluation>> GetByEvaluatorIdAsync(int evaluatorId);
    Task<IEnumerable<Evaluation>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<Evaluation> CreateAsync(Evaluation evaluation);
    Task<Evaluation> UpdateAsync(Evaluation evaluation);
}
