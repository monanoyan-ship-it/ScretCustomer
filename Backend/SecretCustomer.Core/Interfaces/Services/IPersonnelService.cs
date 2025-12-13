using SecretCustomer.Core.DTOs.Personnel;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IPersonnelService
{
    Task<PagedPersonnelResult> GetAllAsync(PersonnelFilterDto filter);
    Task<PersonnelDto?> GetByIdAsync(Guid id);
    Task<PersonnelDto> CreateAsync(CreatePersonnelDto dto);
    Task<PersonnelDto?> UpdateAsync(Guid id, UpdatePersonnelDto dto);
    Task<bool> DeleteAsync(Guid id);
    Task<List<PersonnelDto>> GetByBranchAsync(Guid branchId);
    Task<List<PersonnelDto>> GetByCustomerAsync(Guid customerId);
    Task<bool> ExistsByTcKimlikNoAsync(string tcKimlikNo, Guid? excludeId = null);
    Task<bool> ExistsBySicilNoAsync(string sicilNo, Guid? excludeId = null);
}
