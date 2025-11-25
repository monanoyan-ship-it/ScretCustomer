using SecretCustomer.Core.DTOs.FieldWorker;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IFieldWorkerService
{
    Task<FieldWorkerDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<FieldWorkerDto>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<FieldWorkerDto>> GetActiveAsync();
    Task<FieldWorkerDto> CreateAsync(CreateFieldWorkerDto createDto);
    Task<FieldWorkerDto> UpdateAsync(Guid id, UpdateFieldWorkerDto updateDto);
    Task DeleteAsync(Guid id);
}
