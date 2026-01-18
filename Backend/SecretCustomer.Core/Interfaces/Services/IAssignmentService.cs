using SecretCustomer.Core.DTOs.Assignment;
using SecretCustomer.Core.DTOs.AssignmentPeriod;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IAssignmentService
{
    // ===== TEMEL CRUD =====
    Task<AssignmentDto?> GetByIdAsync(int id);
    Task<AssignmentDetailDto?> GetDetailByIdAsync(int id);
    Task<IEnumerable<AssignmentDto>> GetAllAsync();

    /// <summary>
    /// Performans için optimize edilmiş liste metodu - projection kullanır
    /// </summary>
    Task<IEnumerable<AssignmentListDto>> GetListAsync(
        int? projectId = null,
        int? assignedUserId = null,
        string? status = null,
        string? searchTerm = null);
    Task<AssignmentDto?> GetByUniqueLinkAsync(string uniqueLink);
    Task<AssignmentDto> CreateAsync(CreateAssignmentDto dto);
    Task UpdateAsync(int id, UpdateAssignmentDto dto);
    Task<bool> DeleteAsync(int id);

    // ===== FİLTRELEME =====
    Task<IEnumerable<AssignmentDto>> GetByProjectIdAsync(int projectId);
    Task<IEnumerable<AssignmentDto>> GetByUserIdAsync(int userId);
    Task<IEnumerable<AssignmentDto>> GetByCustomerPersonnelIdAsync(int customerPersonnelId);
    Task<PagedAssignmentResult> GetFilteredAsync(AssignmentFilterDto filter);

    // ===== DURUM YÖNETİMİ =====
    Task<AssignmentDto> CompleteAssignmentAsync(int id);
    Task<AssignmentDto> CancelAssignmentAsync(int id, string? reason);
    Task<AssignmentDto> ReopenAssignmentAsync(int id);
    Task<AssignmentDto> ReassignAsync(int id, ReassignAssignmentDto dto);
    Task<AssignmentDto> UpdateDueDateAsync(int id, DateTime newDueDate);

    // ===== TOPLU İŞLEMLER =====
    Task<int> DeleteByProjectIdAsync(int projectId);

    // ===== İSTATİSTİKLER =====
    Task<AssignmentSummaryDto> GetSummaryAsync(int? projectId = null);
    Task<IEnumerable<ProjectAssignmentSummaryDto>> GetProjectSummariesAsync();

    // ===== SÜRESI DOLANLAR =====
    Task<IEnumerable<AssignmentDto>> GetExpiredAsync();
    Task<IEnumerable<AssignmentDto>> GetUpcomingDueAsync(int daysAhead = 3);

    // ===== DÖNEMLER (PERIODS) =====
    Task<IEnumerable<AssignmentPeriodDto>> GetPeriodsAsync(int assignmentId);
    Task<AssignmentPeriodDto> CreatePeriodAsync(CreateAssignmentPeriodDto dto);
    Task<AssignmentPeriodDto> UpdatePeriodAsync(UpdateAssignmentPeriodDto dto);
    Task<AssignmentPeriodDto> ClosePeriodAsync(int periodId);
    Task<AssignmentPeriodDto> ReopenPeriodAsync(int periodId);
    Task DeletePeriodAsync(int periodId);

    // ===== İÇ DEĞERLENDİRME ATAMALARI =====
    Task<IEnumerable<AssignmentDto>> GetInternalAssignmentsAsync(InternalAssignmentFilterDto filter);
    Task<InternalAssignmentSummaryDto> GetInternalAssignmentSummaryAsync(int? customerId);
    Task<InternalAssignmentResultDto> CreateInternalAssignmentsAsync(CreateInternalAssignmentsDto dto);
}
