using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Repositories;

public interface IEvaluationRepository
{
    Task<Evaluation?> GetByIdAsync(Guid id, bool includeDetails = false);
    Task<Evaluation?> GetByAssignmentIdAsync(Guid assignmentId, bool includeDetails = false);
    Task<IEnumerable<Evaluation>> GetByEvaluatorIdAsync(Guid evaluatorId);
    Task<IEnumerable<Evaluation>> GetByBranchIdAsync(Guid branchId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<Evaluation>> GetAllAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<Evaluation> CreateAsync(Evaluation evaluation);
    Task<Evaluation> UpdateAsync(Evaluation evaluation);
}
