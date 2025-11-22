using SecretCustomer.Core.DTOs.Branch;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IBranchService
{
    Task<BranchDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<BranchDto>> GetAllAsync(bool includeInactive = false);
    Task<IEnumerable<BranchDto>> GetActiveAsync();
    Task<BranchDto?> GetByCodeAsync(string code);
    Task<IEnumerable<BranchDto>> GetByRegionAsync(string region);
    Task<IEnumerable<BranchDto>> GetByCityAsync(string city);
    Task<BranchDto> CreateAsync(CreateBranchDto createBranchDto);
    Task<BranchDto> UpdateAsync(Guid id, UpdateBranchDto updateBranchDto);
    Task DeleteAsync(Guid id);
}
