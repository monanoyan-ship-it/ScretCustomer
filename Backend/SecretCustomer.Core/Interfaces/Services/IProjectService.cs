using SecretCustomer.Core.DTOs.Project;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IProjectService
{
    Task<ProjectDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ProjectDto>> GetAllAsync(bool includeInactive = false);
    Task<ProjectDto> CreateAsync(CreateProjectDto dto);
    Task<ProjectDto> UpdateAsync(Guid id, CreateProjectDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<ProjectDto> CloseProjectAsync(Guid id);
}
