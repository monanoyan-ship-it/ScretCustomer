using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ISurveyInvitationDataService
{
    Task<SurveyInvitation?> GetByIdAsync(int id);
    Task<List<SurveyInvitation>> GetAllAsync();
    Task AddAsync(SurveyInvitation entity);
    Task UpdateAsync(SurveyInvitation entity);
    Task DeleteAsync(int id);
}
