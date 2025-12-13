using SecretCustomer.Core.DTOs.Organization;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IOrganizationService
{
    // OrganizationUnit methods
    Task<List<OrganizationUnitDto>> GetAllOrganizationUnitsAsync();
    Task<OrganizationUnitDto?> GetOrganizationUnitByIdAsync(Guid id);
    Task<List<OrganizationTreeNodeDto>> GetOrganizationTreeAsync();
    Task<List<OrganizationUnitDto>> GetOrganizationUnitsAsync(OrganizationUnitFilterDto filter);
    Task<OrganizationUnitDto> CreateOrganizationUnitAsync(CreateOrganizationUnitDto dto);
    Task<OrganizationUnitDto> UpdateOrganizationUnitAsync(Guid id, UpdateOrganizationUnitDto dto);
    Task<bool> DeleteOrganizationUnitAsync(Guid id);
    Task<List<OrganizationUnitDto>> GetChildrenAsync(Guid parentId);
    Task<bool> MoveOrganizationUnitAsync(Guid id, Guid? newParentId);

    // Delegation methods
    Task<List<DelegationDto>> GetAllDelegationsAsync();
    Task<DelegationDto?> GetDelegationByIdAsync(Guid id);
    Task<PagedDelegationResult> GetDelegationsAsync(DelegationFilterDto filter);
    Task<DelegationDto> CreateDelegationAsync(CreateDelegationDto dto);
    Task<DelegationDto> UpdateDelegationAsync(Guid id, UpdateDelegationDto dto);
    Task<bool> DeleteDelegationAsync(Guid id);
    Task<DelegationDto> ApproveDelegationAsync(Guid id, Guid approverUserId, ApproveDelegationDto dto);
    Task<List<DelegationDto>> GetActiveDelegationsForUserAsync(Guid userId);
    Task<List<DelegationDto>> GetDelegationsGivenByUserAsync(Guid userId);
    Task<List<DelegationDto>> GetDelegationsReceivedByUserAsync(Guid userId);
}
