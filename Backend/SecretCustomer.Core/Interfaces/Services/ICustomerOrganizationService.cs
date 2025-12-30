using SecretCustomer.Core.DTOs.CustomerOrganization;

namespace SecretCustomer.Core.Interfaces.Services;

/// <summary>
/// Müşteri organizasyon yönetimi servisi
/// </summary>
public interface ICustomerOrganizationService
{
    // Organizasyon CRUD
    Task<CustomerOrganizationDto?> GetByIdAsync(int id);
    Task<IEnumerable<CustomerOrganizationDto>> GetByCustomerIdAsync(int customerId, bool includeInactive = false);
    Task<OrganizationTreeDto?> GetOrganizationTreeAsync(int customerId);
    Task<CustomerOrganizationDto> CreateAsync(CreateCustomerOrganizationDto dto);
    Task<CustomerOrganizationDto> UpdateAsync(int id, UpdateCustomerOrganizationDto dto);
    Task DeleteAsync(int id);

    // Personel yönetimi
    Task<OrganizationPersonnelListDto> GetPersonnelByOrganizationIdAsync(int organizationId);
    Task<IEnumerable<OrganizationPersonnelSummaryDto>> GetPersonnelPoolAsync(int customerId);
    Task AssignPersonnelToOrganizationAsync(AssignPersonnelToOrganizationDto dto);
    Task RemovePersonnelFromOrganizationAsync(int personnelId, int organizationId);
    Task SetSupervisorAsync(int personnelId, int? supervisorId);
    Task TransferTeamAndRemoveAsync(int organizationId, int personnelIdToRemove, int newSupervisorId);
}
