using SecretCustomer.Core.Entities;

namespace SecretCustomer.Core.Interfaces.Services.DataServices;

public interface IProjectTeamMemberDataService
{
    Task<ProjectTeamMember?> GetByIdAsync(int id);
    Task<List<ProjectTeamMember>> GetAllAsync();
    Task AddAsync(ProjectTeamMember entity);
    Task UpdateAsync(ProjectTeamMember entity);
    Task DeleteAsync(int id);
}
