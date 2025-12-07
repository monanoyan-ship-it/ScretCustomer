using SecretCustomer.Core.DTOs.Assignment;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IAssignmentService
{
    Task<AssignmentDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<AssignmentDto>> GetAllAsync();
    Task<AssignmentDto?> GetByUniqueLinkAsync(string uniqueLink);
    Task<IEnumerable<AssignmentDto>> GetByProjectIdAsync(Guid projectId);
    Task<IEnumerable<AssignmentDto>> GetByUserIdAsync(Guid userId);
    Task<IEnumerable<AssignmentDto>> GetByBranchIdAsync(Guid branchId);
    Task<IEnumerable<AssignmentDto>> GetByFieldWorkerIdAsync(Guid fieldWorkerId);
    Task<AssignmentDto> CreateAsync(CreateAssignmentDto dto);
    Task<IEnumerable<AssignmentDto>> CreateBulkAsync(BulkAssignmentDto dto);
    Task UpdateAsync(Guid id, UpdateAssignmentDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<AssignmentDto> CompleteAssignmentAsync(Guid id);
}
