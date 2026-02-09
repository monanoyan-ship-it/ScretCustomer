using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IQuestionAttachmentDataService
{
    Task<QuestionAttachment?> GetByIdAsync(int id);
    Task<List<QuestionAttachment>> GetAllAsync();
    Task AddAsync(QuestionAttachment entity);
    Task UpdateAsync(QuestionAttachment entity);
    Task DeleteAsync(int id);
}
