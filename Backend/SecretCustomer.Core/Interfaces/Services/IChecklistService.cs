using SecretCustomer.Core.DTOs.Checklist;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IChecklistService
{
    Task<ChecklistDto?> GetByIdAsync(int id);
    Task<IEnumerable<ChecklistDto>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<ChecklistDto>> GetFilteredAsync(string? searchText = null, int? customerId = null, int? customerOrganizationId = null, bool includeInactive = false);
    Task<ChecklistDto> CreateAsync(CreateChecklistDto dto);
    Task<ChecklistDto> UpdateAsync(UpdateChecklistDto dto);
    Task<bool> DeleteAsync(int id);
    Task<ChecklistDto> CloneChecklistAsync(int id, string newName);
}
