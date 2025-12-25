using SecretCustomer.Core.DTOs.Personnel;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IPersonnelService
{
    Task<PagedPersonnelResult> GetAllAsync(PersonnelFilterDto filter);
    Task<PersonnelDto?> GetByIdAsync(int id);
    Task<PersonnelDto> CreateAsync(CreatePersonnelDto dto);
    Task<PersonnelDto?> UpdateAsync(int id, UpdatePersonnelDto dto);
    Task<bool> DeleteAsync(int id);
    Task<List<PersonnelDto>> GetByBranchAsync(int branchId);
    Task<List<PersonnelDto>> GetByCustomerAsync(int customerId);
    Task<bool> ExistsByTcKimlikNoAsync(string tcKimlikNo, int? excludeId = null);
    Task<bool> ExistsBySicilNoAsync(string sicilNo, int? excludeId = null);
}
