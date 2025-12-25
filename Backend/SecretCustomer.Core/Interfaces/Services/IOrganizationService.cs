using SecretCustomer.Core.DTOs.Organization;

namespace SecretCustomer.Core.Interfaces.Services;

public interface IOrganizationService
{
    // OrganizationUnit methods
    Task<List<OrganizationUnitDto>> GetAllOrganizationUnitsAsync();
    Task<OrganizationUnitDto?> GetOrganizationUnitByIdAsync(int id);
    Task<List<OrganizationTreeNodeDto>> GetOrganizationTreeAsync();
    Task<List<OrganizationUnitDto>> GetOrganizationUnitsAsync(OrganizationUnitFilterDto filter);
    Task<OrganizationUnitDto> CreateOrganizationUnitAsync(CreateOrganizationUnitDto dto);
    Task<OrganizationUnitDto> UpdateOrganizationUnitAsync(int id, UpdateOrganizationUnitDto dto);
    Task<bool> DeleteOrganizationUnitAsync(int id);
    Task<List<OrganizationUnitDto>> GetChildrenAsync(int parentId);
    Task<bool> MoveOrganizationUnitAsync(int id, int? newParentId);

    // Delegation methods
    Task<List<DelegationDto>> GetAllDelegationsAsync();
    Task<DelegationDto?> GetDelegationByIdAsync(int id);
    Task<PagedDelegationResult> GetDelegationsAsync(DelegationFilterDto filter);
    Task<DelegationDto> CreateDelegationAsync(CreateDelegationDto dto);
    Task<DelegationDto> UpdateDelegationAsync(int id, UpdateDelegationDto dto);
    Task<bool> DeleteDelegationAsync(int id);
    Task<DelegationDto> ApproveDelegationAsync(int id, int approverUserId, ApproveDelegationDto dto);
    Task<List<DelegationDto>> GetActiveDelegationsForUserAsync(int userId);
    Task<List<DelegationDto>> GetDelegationsGivenByUserAsync(int userId);
    Task<List<DelegationDto>> GetDelegationsReceivedByUserAsync(int userId);
}
