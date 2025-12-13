using SecretCustomer.Core.DTOs.Project;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IProjectService
{
    Task<ProjectDto?> GetByIdAsync(Guid id);
    Task<ProjectDetailDto?> GetDetailByIdAsync(Guid id);
    Task<IEnumerable<ProjectDto>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<ProjectSummaryDto>> GetSummariesAsync();
    Task<ProjectDto> CreateAsync(CreateProjectDto dto);
    Task<ProjectDto> UpdateAsync(Guid id, CreateProjectDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<ProjectDto> CloseProjectAsync(Guid id);

    // ===== YENI METOTLAR =====

    /// <summary>
    /// Proje durumunu guncelle
    /// </summary>
    Task<ProjectDto> UpdateStatusAsync(Guid id, UpdateProjectStatusDto dto);

    /// <summary>
    /// Projeyi baslat
    /// </summary>
    Task<ProjectDto> StartProjectAsync(Guid id);

    /// <summary>
    /// Projeyi duraksat
    /// </summary>
    Task<ProjectDto> PauseProjectAsync(Guid id);

    /// <summary>
    /// Projeyi tamamla
    /// </summary>
    Task<ProjectDto> CompleteProjectAsync(Guid id);

    /// <summary>
    /// Projeyi iptal et
    /// </summary>
    Task<ProjectDto> CancelProjectAsync(Guid id, string? reason);

    /// <summary>
    /// Proje takimini yonet
    /// </summary>
    Task<ProjectDetailDto> ManageTeamAsync(Guid projectId, ManageProjectTeamDto dto);

    /// <summary>
    /// Proje subelerini yonet
    /// </summary>
    Task<ProjectDetailDto> ManageBranchesAsync(Guid projectId, ManageProjectBranchesDto dto);

    /// <summary>
    /// Proje istatistiklerini getir
    /// </summary>
    Task<ProjectDetailDto> GetStatisticsAsync(Guid projectId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Musteri bazli projeleri getir
    /// </summary>
    Task<IEnumerable<ProjectDto>> GetByCustomerIdAsync(Guid customerId);

    /// <summary>
    /// Proje yoneticisi bazli projeleri getir
    /// </summary>
    Task<IEnumerable<ProjectDto>> GetByManagerIdAsync(Guid managerId);

    /// <summary>
    /// Aktif projeleri getir
    /// </summary>
    Task<IEnumerable<ProjectDto>> GetActiveProjectsAsync();

    /// <summary>
    /// Yaklasan bitis tarihli projeleri getir
    /// </summary>
    Task<IEnumerable<ProjectDto>> GetUpcomingDeadlinesAsync(int daysAhead = 7);

    /// <summary>
    /// Proje kodu olustur
    /// </summary>
    Task<string> GenerateProjectCodeAsync();
}
