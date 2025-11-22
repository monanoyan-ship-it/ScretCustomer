using SecretCustomer.Core.DTOs.Branch;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class BranchService : IBranchService
{
    private readonly IBranchRepository _branchRepository;

    public BranchService(IBranchRepository branchRepository)
    {
        _branchRepository = branchRepository;
    }

    public async Task<BranchDto?> GetByIdAsync(Guid id)
    {
        var branch = await _branchRepository.GetByIdAsync(id, includeDetails: true);
        return branch == null ? null : MapToDto(branch);
    }

    public async Task<IEnumerable<BranchDto>> GetAllAsync(bool includeInactive = false)
    {
        var branches = await _branchRepository.GetAllAsync(includeInactive);
        return branches.Select(MapToDto);
    }

    public async Task<IEnumerable<BranchDto>> GetActiveAsync()
    {
        var branches = await _branchRepository.GetActiveAsync();
        return branches.Select(MapToDto);
    }

    public async Task<BranchDto?> GetByCodeAsync(string code)
    {
        var branch = await _branchRepository.GetByCodeAsync(code);
        return branch == null ? null : MapToDto(branch);
    }

    public async Task<IEnumerable<BranchDto>> GetByRegionAsync(string region)
    {
        var branches = await _branchRepository.GetByRegionAsync(region);
        return branches.Select(MapToDto);
    }

    public async Task<IEnumerable<BranchDto>> GetByCityAsync(string city)
    {
        var branches = await _branchRepository.GetByCityAsync(city);
        return branches.Select(MapToDto);
    }

    public async Task<BranchDto> CreateAsync(CreateBranchDto createBranchDto)
    {
        // Check if code already exists
        if (await _branchRepository.ExistsByCodeAsync(createBranchDto.Code))
        {
            throw new InvalidOperationException("Branch code already exists");
        }

        var branch = new Branch
        {
            Name = createBranchDto.Name,
            Code = createBranchDto.Code,
            Address = createBranchDto.Address,
            City = createBranchDto.City,
            Region = createBranchDto.Region,
            IsActive = createBranchDto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        var createdBranch = await _branchRepository.CreateAsync(branch);
        return MapToDto(createdBranch);
    }

    public async Task<BranchDto> UpdateAsync(Guid id, UpdateBranchDto updateBranchDto)
    {
        var branch = await _branchRepository.GetByIdAsync(id);
        if (branch == null)
        {
            throw new KeyNotFoundException($"Branch with ID {id} not found");
        }

        // Check if code is being changed and if it's already in use
        if (branch.Code != updateBranchDto.Code &&
            await _branchRepository.ExistsByCodeAsync(updateBranchDto.Code, id))
        {
            throw new InvalidOperationException("Branch code already exists");
        }

        branch.Name = updateBranchDto.Name;
        branch.Code = updateBranchDto.Code;
        branch.Address = updateBranchDto.Address;
        branch.City = updateBranchDto.City;
        branch.Region = updateBranchDto.Region;
        branch.IsActive = updateBranchDto.IsActive;

        var updatedBranch = await _branchRepository.UpdateAsync(branch);
        return MapToDto(updatedBranch);
    }

    public async Task DeleteAsync(Guid id)
    {
        var branch = await _branchRepository.GetByIdAsync(id);
        if (branch == null)
        {
            throw new KeyNotFoundException($"Branch with ID {id} not found");
        }

        await _branchRepository.DeleteAsync(id);
    }

    private static BranchDto MapToDto(Branch branch)
    {
        return new BranchDto
        {
            Id = branch.Id,
            Name = branch.Name,
            Code = branch.Code,
            Address = branch.Address,
            City = branch.City,
            Region = branch.Region,
            IsActive = branch.IsActive,
            UserCount = branch.Users?.Count ?? 0,
            AssignmentCount = branch.Assignments?.Count ?? 0,
            CreatedAt = branch.CreatedAt,
            UpdatedAt = branch.UpdatedAt
        };
    }
}
