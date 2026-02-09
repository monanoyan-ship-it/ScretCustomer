using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface ISurveyExternalInvitationDataService
{
    Task<SurveyExternalInvitation?> GetByIdAsync(int id);
    Task<List<SurveyExternalInvitation>> GetAllAsync();
    Task AddAsync(SurveyExternalInvitation entity);
    Task UpdateAsync(SurveyExternalInvitation entity);
    Task DeleteAsync(int id);
}
