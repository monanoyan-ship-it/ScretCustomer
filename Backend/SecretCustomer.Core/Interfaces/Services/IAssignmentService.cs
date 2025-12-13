using SecretCustomer.Core.DTOs.Assignment;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IAssignmentService
{
    // ===== TEMEL CRUD =====
    Task<AssignmentDto?> GetByIdAsync(Guid id);
    Task<AssignmentDetailDto?> GetDetailByIdAsync(Guid id);
    Task<IEnumerable<AssignmentDto>> GetAllAsync();
    Task<AssignmentDto?> GetByUniqueLinkAsync(string uniqueLink);
    Task<AssignmentDto> CreateAsync(CreateAssignmentDto dto);
    Task<IEnumerable<AssignmentDto>> CreateBulkAsync(BulkAssignmentDto dto);
    Task UpdateAsync(Guid id, UpdateAssignmentDto dto);
    Task<bool> DeleteAsync(Guid id);

    // ===== FİLTRELEME =====
    Task<IEnumerable<AssignmentDto>> GetByProjectIdAsync(Guid projectId);
    Task<IEnumerable<AssignmentDto>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<AssignmentDto>> GetByBranchIdAsync(Guid branchId);
    Task<IEnumerable<AssignmentDto>> GetByFieldWorkerIdAsync(Guid fieldWorkerId);
    Task<IEnumerable<AssignmentDto>> GetFilteredAsync(AssignmentFilterDto filter);

    // ===== DURUM YÖNETİMİ =====
    Task<AssignmentDto> CompleteAssignmentAsync(Guid id);
    Task<AssignmentDto> CancelAssignmentAsync(Guid id, string? reason);
    Task<AssignmentDto> ReassignAsync(Guid id, ReassignAssignmentDto dto);

    // ===== TOPLU İŞLEMLER =====
    Task<IEnumerable<AssignmentDto>> CreateForProjectBranchesAsync(BulkProjectAssignmentDto dto);
    Task<int> DeleteByProjectIdAsync(Guid projectId);

    // ===== İSTATİSTİKLER =====
    Task<AssignmentSummaryDto> GetSummaryAsync(Guid? projectId = null);
    Task<IEnumerable<ProjectAssignmentSummaryDto>> GetProjectSummariesAsync();
    Task<IEnumerable<BranchAssignmentSummaryDto>> GetBranchSummariesAsync(Guid projectId);

    // ===== SÜRESI DOLANLAR =====
    Task<IEnumerable<AssignmentDto>> GetExpiredAsync();
    Task<IEnumerable<AssignmentDto>> GetUpcomingDueAsync(int daysAhead = 3);
}
