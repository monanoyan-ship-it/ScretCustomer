using SecretCustomer.Core.DTOs.Assignment;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IAssignmentService
{
    // ===== TEMEL CRUD =====
    Task<AssignmentDto?> GetByIdAsync(int id);
    Task<AssignmentDetailDto?> GetDetailByIdAsync(int id);
    Task<IEnumerable<AssignmentDto>> GetAllAsync();
    Task<AssignmentDto?> GetByUniqueLinkAsync(string uniqueLink);
    Task<AssignmentDto> CreateAsync(CreateAssignmentDto dto);
    Task<IEnumerable<AssignmentDto>> CreateBulkAsync(BulkAssignmentDto dto);
    Task UpdateAsync(int id, UpdateAssignmentDto dto);
    Task<bool> DeleteAsync(int id);

    // ===== FİLTRELEME =====
    Task<IEnumerable<AssignmentDto>> GetByProjectIdAsync(int projectId);
    Task<IEnumerable<AssignmentDto>> GetByUserIdAsync(int userId);
    Task<IEnumerable<AssignmentDto>> GetByBranchIdAsync(int branchId);
    Task<IEnumerable<AssignmentDto>> GetByFieldWorkerIdAsync(int fieldWorkerId);
    Task<IEnumerable<AssignmentDto>> GetFilteredAsync(AssignmentFilterDto filter);

    // ===== DURUM YÖNETİMİ =====
    Task<AssignmentDto> CompleteAssignmentAsync(int id);
    Task<AssignmentDto> CancelAssignmentAsync(int id, string? reason);
    Task<AssignmentDto> ReopenAssignmentAsync(int id);
    Task<AssignmentDto> ReassignAsync(int id, ReassignAssignmentDto dto);

    // ===== TOPLU İŞLEMLER =====
    Task<IEnumerable<AssignmentDto>> CreateForProjectBranchesAsync(BulkProjectAssignmentDto dto);
    Task<int> DeleteByProjectIdAsync(int projectId);

    // ===== İSTATİSTİKLER =====
    Task<AssignmentSummaryDto> GetSummaryAsync(int? projectId = null);
    Task<IEnumerable<ProjectAssignmentSummaryDto>> GetProjectSummariesAsync();
    Task<IEnumerable<BranchAssignmentSummaryDto>> GetBranchSummariesAsync(int projectId);

    // ===== SÜRESI DOLANLAR =====
    Task<IEnumerable<AssignmentDto>> GetExpiredAsync();
    Task<IEnumerable<AssignmentDto>> GetUpcomingDueAsync(int daysAhead = 3);
}
