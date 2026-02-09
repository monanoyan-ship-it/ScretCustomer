using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IEvaluationAttachmentDataService
{
    Task<EvaluationAttachment?> GetByIdAsync(int id);
    Task<List<EvaluationAttachment>> GetAllAsync();
    Task AddAsync(EvaluationAttachment entity);
    Task UpdateAsync(EvaluationAttachment entity);
    Task DeleteAsync(int id);
}
