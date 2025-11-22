using SecretCustomer.Core.DTOs.Evaluation;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IEvaluationService
{
    Task<EvaluationDto?> GetByIdAsync(Guid id);
    Task<EvaluationDto?> GetByAssignmentIdAsync(Guid assignmentId);
    Task<IEnumerable<EvaluationDto>> GetByEvaluatorIdAsync(Guid evaluatorId);
    Task<EvaluationDto> SubmitEvaluationAsync(SubmitEvaluationDto dto);
    Task<EvaluationDto> StartEvaluationAsync(Guid assignmentId, Guid? evaluatorId);
}
