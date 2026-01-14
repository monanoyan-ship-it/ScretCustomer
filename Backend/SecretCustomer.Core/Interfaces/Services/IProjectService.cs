using SecretCustomer.Core.DTOs.Project;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IProjectService
{
    Task<ProjectDto?> GetByIdAsync(int id);
    Task<ProjectDetailDto?> GetDetailByIdAsync(int id);
    Task<IEnumerable<ProjectDto>> GetAllAsync(bool includeInactive = false);
    /// <summary>
    /// Liste görünümü için optimize edilmiş method (Assignments/TeamMembers yüklemez)
    /// </summary>
    Task<IEnumerable<ProjectListDto>> GetListAsync(
        string? searchText = null,
        int? customerId = null,
        string? projectType = null,
        string? status = null,
        int? projectManagerId = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        bool includeInactive = false);
    Task<IEnumerable<ProjectSummaryDto>> GetSummariesAsync();
    Task<ProjectDto> CreateAsync(CreateProjectDto dto);
    Task<ProjectDto> UpdateAsync(int id, CreateProjectDto dto);
    Task<bool> DeleteAsync(int id);
    Task<ProjectDto> CloseProjectAsync(int id);

    // ===== YENI METOTLAR =====

    /// <summary>
    /// Proje durumunu guncelle
    /// </summary>
    Task<ProjectDto> UpdateStatusAsync(int id, UpdateProjectStatusDto dto);

    /// <summary>
    /// Projeyi baslat
    /// </summary>
    Task<ProjectDto> StartProjectAsync(int id);

    /// <summary>
    /// Projeyi duraksat
    /// </summary>
    Task<ProjectDto> PauseProjectAsync(int id);

    /// <summary>
    /// Projeyi tamamla
    /// </summary>
    Task<ProjectDto> CompleteProjectAsync(int id);

    /// <summary>
    /// Projeyi iptal et
    /// </summary>
    Task<ProjectDto> CancelProjectAsync(int id, string? reason);

    /// <summary>
    /// Proje takimini yonet
    /// </summary>
    Task<ProjectDetailDto> ManageTeamAsync(int projectId, ManageProjectTeamDto dto);

    /// <summary>
    /// Proje subelerini yonet
    /// </summary>
    Task<ProjectDetailDto> ManageBranchesAsync(int projectId, ManageProjectBranchesDto dto);

    /// <summary>
    /// Proje istatistiklerini getir
    /// </summary>
    Task<ProjectDetailDto> GetStatisticsAsync(int projectId, DateTime? startDate = null, DateTime? endDate = null);

    /// <summary>
    /// Musteri bazli projeleri getir
    /// </summary>
    Task<IEnumerable<ProjectDto>> GetByCustomerIdAsync(int customerId);

    /// <summary>
    /// Proje yoneticisi bazli projeleri getir
    /// </summary>
    Task<IEnumerable<ProjectDto>> GetByManagerIdAsync(int managerId);

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
