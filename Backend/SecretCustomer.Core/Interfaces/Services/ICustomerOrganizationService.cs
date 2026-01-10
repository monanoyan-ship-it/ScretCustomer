using SecretCustomer.Core.DTOs.CustomerOrganization;

namespace SecretCustomer.Core.Interfaces.Services;

/// <summary>
/// Müşteri organizasyon yönetimi servisi
/// </summary>
public interface ICustomerOrganizationService
{
    // Debug
    Task<object> DebugCheckPersonnelAsync();

    // Organizasyon CRUD
    Task<CustomerOrganizationDto?> GetByIdAsync(int id);
    Task<IEnumerable<CustomerOrganizationDto>> GetByCustomerIdAsync(int customerId, bool includeInactive = false);
    Task<OrganizationTreeDto?> GetOrganizationTreeAsync(int customerId);
    Task<CustomerOrganizationDto> CreateAsync(CreateCustomerOrganizationDto dto);
    Task<CustomerOrganizationDto> UpdateAsync(int id, UpdateCustomerOrganizationDto dto);
    Task DeleteAsync(int id);

    /// <summary>
    /// Organizasyonun parent'ını değiştir (Drag & Drop için)
    /// </summary>
    Task MoveOrganizationAsync(int organizationId, int? newParentId);

    // Personel yönetimi
    Task<OrganizationPersonnelListDto> GetPersonnelByOrganizationIdAsync(int organizationId);
    Task<IEnumerable<OrganizationPersonnelSummaryDto>> GetPersonnelPoolAsync(int customerId);
    Task AssignPersonnelToOrganizationAsync(AssignPersonnelToOrganizationDto dto);
    Task RemovePersonnelFromOrganizationAsync(int personnelId, int organizationId);
    Task SetSupervisorAsync(int personnelId, int? supervisorId);
    Task TransferTeamAndRemoveAsync(int organizationId, int personnelIdToRemove, int newSupervisorId);

    // ===== Coklu Organizasyon Destegi (Junction Table) =====

    /// <summary>
    /// Personelin gorev aldigi tum organizasyonlari getir
    /// </summary>
    Task<IEnumerable<PersonnelOrganizationAssignmentDto>> GetPersonnelOrganizationsAsync(int personnelId);

    /// <summary>
    /// Personeli bir organizasyona ata (junction table uzerinden)
    /// </summary>
    Task<PersonnelOrganizationAssignmentDto> AddPersonnelToOrganizationAsync(int personnelId, int organizationId, int? supervisorId = null, string? notes = null);

    /// <summary>
    /// Personelin bir organizasyondaki atamasini guncelle (supervisor degistirme vb.)
    /// </summary>
    Task<PersonnelOrganizationAssignmentDto> UpdatePersonnelOrganizationAssignmentAsync(int personnelId, int organizationId, UpdatePersonnelOrganizationAssignmentDto dto);

    /// <summary>
    /// Personeli bir organizasyondan cikar (junction table uzerinden)
    /// </summary>
    Task RemovePersonnelFromOrganizationV2Async(int personnelId, int organizationId);

    /// <summary>
    /// Personeli belirli bir süpervizörün ekibinden çıkar
    /// </summary>
    Task RemoveFromTeamAsync(int personnelId, int organizationId, int supervisorId);

    /// <summary>
    /// Organizasyondaki tum personel atamalarini getir (junction table uzerinden)
    /// </summary>
    Task<IEnumerable<PersonnelOrganizationAssignmentDto>> GetOrganizationPersonnelAssignmentsAsync(int organizationId);
}
