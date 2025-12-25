using SecretCustomer.Core.DTOs.FieldWorker;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IFieldWorkerService
{
    Task<FieldWorkerDto?> GetByIdAsync(int id);
    Task<FieldWorkerDto?> GetByUserIdAsync(int userId);
    Task<IEnumerable<FieldWorkerDto>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<FieldWorkerDto>> GetActiveAsync();
    Task<FieldWorkerDto> CreateAsync(CreateFieldWorkerDto createDto);
    Task<FieldWorkerDto> UpdateAsync(int id, UpdateFieldWorkerDto updateDto);
    Task DeleteAsync(int id);
}
