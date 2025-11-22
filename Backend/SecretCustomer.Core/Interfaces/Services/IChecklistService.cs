using SecretCustomer.Core.DTOs.Checklist;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IChecklistService
{
    Task<ChecklistDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ChecklistDto>> GetAllAsync(bool includeInactive = false);
    Task<ChecklistDto> CreateAsync(CreateChecklistDto dto);
    Task<ChecklistDto> UpdateAsync(UpdateChecklistDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<ChecklistDto> CloneChecklistAsync(Guid id, string newName);
}
