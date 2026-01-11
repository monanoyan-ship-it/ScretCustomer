using SecretCustomer.Core.DTOs.Customer;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Repositories;
using SecretCustomer.Core.Interfaces.Services;

namespace SecretCustomer.Services.Services;

public class CustomerPersonnelService : ICustomerPersonnelService
{
    private readonly ICustomerPersonnelRepository _personnelRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerPersonnelOrganizationRepository _personnelOrgRepository;
    private readonly ILocalizationService _localizationService;

    public CustomerPersonnelService(
        ICustomerPersonnelRepository personnelRepository,
        ICustomerRepository customerRepository,
        ICustomerPersonnelOrganizationRepository personnelOrgRepository,
        ILocalizationService localizationService)
    {
        _personnelRepository = personnelRepository;
        _customerRepository = customerRepository;
        _personnelOrgRepository = personnelOrgRepository;
        _localizationService = localizationService;
    }

    private async Task<string> GetRoleNameAsync(int roleId)
    {
        var item = CustomerPersonnelRoles.GetById(roleId);
        if (item == null) return "Bilinmeyen";
        return await _localizationService.GetResourceAsync(item.NameResourceKey, (int?)null, item.Description);
    }

    public async Task<CustomerPersonnelDto?> GetByIdAsync(int id)
    {
        var personnel = await _personnelRepository.GetByIdAsync(id, includeDetails: true);
        return personnel == null ? null : await MapToDtoAsync(personnel);
    }

    public async Task<IEnumerable<CustomerPersonnelDto>> GetAllAsync(bool includeInactive = false)
    {
        var personnel = await _personnelRepository.GetAllAsync(includeInactive);
        var result = new List<CustomerPersonnelDto>();
        foreach (var p in personnel)
        {
            result.Add(await MapToDtoAsync(p));
        }
        return result;
    }

    public async Task<IEnumerable<CustomerPersonnelDto>> GetByCustomerIdAsync(int customerId, bool includeInactive = false)
    {
        var personnel = await _personnelRepository.GetByCustomerIdAsync(customerId, includeInactive);
        var result = new List<CustomerPersonnelDto>();
        foreach (var p in personnel)
        {
            result.Add(await MapToDtoAsync(p));
        }
        return result;
    }

    public async Task<IEnumerable<CustomerPersonnelDto>> GetByOrganizationIdAsync(int organizationId, bool includeInactive = false)
    {
        var personnel = await _personnelRepository.GetByOrganizationIdAsync(organizationId, includeInactive);
        var result = new List<CustomerPersonnelDto>();
        foreach (var p in personnel)
        {
            result.Add(await MapToDtoAsync(p));
        }
        return result;
    }

    public async Task ChangeOrganizationAsync(int personnelId, int newOrganizationId)
    {
        var personnel = await _personnelRepository.GetByIdAsync(personnelId);
        if (personnel == null)
        {
            throw new KeyNotFoundException($"Personel bulunamadı (ID: {personnelId})");
        }

        // Mevcut tüm organizasyon atamalarını sil
        var existingAssignments = await _personnelOrgRepository.GetByPersonnelIdAsync(personnelId);
        foreach (var assignment in existingAssignments)
        {
            await _personnelOrgRepository.DeleteAsync(personnelId, assignment.CustomerOrganizationId);
        }

        // Yeni organizasyona ata
        var newAssignment = new CustomerPersonnelOrganization
        {
            CustomerPersonnelId = personnelId,
            CustomerOrganizationId = newOrganizationId,
            AssignedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await _personnelOrgRepository.AddAsync(newAssignment);
    }

    public async Task<CustomerPersonnelDto?> GetByUsernameAsync(string username)
    {
        var personnel = await _personnelRepository.GetByUsernameAsync(username);
        return personnel == null ? null : await MapToDtoAsync(personnel);
    }

    public async Task<CustomerPersonnelDto> CreateAsync(CreateCustomerPersonnelDto createDto)
    {
        // Check if customer exists
        var customer = await _customerRepository.GetByIdAsync(createDto.CustomerId);
        if (customer == null)
        {
            throw new KeyNotFoundException($"Müşteri bulunamadı (ID: {createDto.CustomerId})");
        }

        // Check if username already exists
        if (await _personnelRepository.ExistsByUsernameAsync(createDto.Username))
        {
            throw new InvalidOperationException("Bu kullanıcı adı ile kayıtlı bir personel zaten mevcut");
        }

        // Email yoksa username@temp.com kullan
        var email = string.IsNullOrWhiteSpace(createDto.Email)
            ? $"{createDto.Username}@temp.com"
            : createDto.Email;

        // Check if email already exists (firma bazlı)
        if (await ExistsByEmailInCompanyAsync(email, createDto.CustomerId))
        {
            throw new InvalidOperationException("Bu e-posta adresi ile kayıtlı bir personel zaten mevcut");
        }

        var personnel = new CustomerPersonnel
        {
            CustomerId = createDto.CustomerId,
            Username = createDto.Username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(createDto.Password),
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            PhoneNumber = createDto.PhoneNumber,
            Department = createDto.Department,
            Title = createDto.Title,
            RoleId = CustomerPersonnelRoles.GetBySystemName(createDto.Role)?.Id ?? CustomerPersonnelRoles.Ids.Operator,
            IsActive = createDto.IsActive,
            Notes = createDto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var createdPersonnel = await _personnelRepository.CreateAsync(personnel);

        // Reload with details
        var result = await _personnelRepository.GetByIdAsync(createdPersonnel.Id, includeDetails: true);
        return await MapToDtoAsync(result!);
    }

    public async Task<CustomerPersonnelDto> UpdateAsync(int id, UpdateCustomerPersonnelDto updateDto)
    {
        var personnel = await _personnelRepository.GetByIdAsync(id, includeDetails: true);
        if (personnel == null)
        {
            throw new KeyNotFoundException($"Personel bulunamadı (ID: {id})");
        }

        // Check if username is being changed and if it's already in use
        if (personnel.Username != updateDto.Username &&
            await _personnelRepository.ExistsByUsernameAsync(updateDto.Username, id))
        {
            throw new InvalidOperationException("Bu kullanıcı adı ile kayıtlı bir personel zaten mevcut");
        }

        // Check if email is being changed and if it's already in use
        if (personnel.Email != updateDto.Email &&
            await _personnelRepository.ExistsByEmailAsync(updateDto.Email, id))
        {
            throw new InvalidOperationException("Bu e-posta adresi ile kayıtlı bir personel zaten mevcut");
        }

        personnel.Username = updateDto.Username;
        personnel.Email = updateDto.Email;

        // Only update password if provided
        if (!string.IsNullOrEmpty(updateDto.Password))
        {
            personnel.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateDto.Password);
        }

        personnel.FirstName = updateDto.FirstName;
        personnel.LastName = updateDto.LastName;
        personnel.PhoneNumber = updateDto.PhoneNumber;
        personnel.Department = updateDto.Department;
        personnel.Title = updateDto.Title;
        personnel.RoleId = CustomerPersonnelRoles.GetBySystemName(updateDto.Role)?.Id ?? CustomerPersonnelRoles.Ids.Operator;
        personnel.IsActive = updateDto.IsActive;
        personnel.Notes = updateDto.Notes;

        var updatedPersonnel = await _personnelRepository.UpdateAsync(personnel);

        // Reload with details
        var result = await _personnelRepository.GetByIdAsync(updatedPersonnel.Id, includeDetails: true);
        return await MapToDtoAsync(result!);
    }

    public async Task DeleteAsync(int id)
    {
        var personnel = await _personnelRepository.GetByIdAsync(id);
        if (personnel == null)
        {
            throw new KeyNotFoundException($"Personel bulunamadı (ID: {id})");
        }

        await _personnelRepository.DeleteAsync(id);
    }

    public async Task ChangePasswordAsync(int personnelId, CustomerPersonnelChangePasswordDto changePasswordDto)
    {
        var personnel = await _personnelRepository.GetByIdAsync(personnelId);
        if (personnel == null)
        {
            throw new KeyNotFoundException($"Personel bulunamadı (ID: {personnelId})");
        }

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, personnel.PasswordHash))
        {
            throw new InvalidOperationException("Mevcut şifre yanlış.");
        }

        // Update password
        personnel.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
        personnel.UpdatedAt = DateTime.UtcNow;

        await _personnelRepository.UpdateAsync(personnel);
    }

    public async Task ResetPasswordAsync(int personnelId, AdminResetPasswordDto resetPasswordDto)
    {
        var personnel = await _personnelRepository.GetByIdAsync(personnelId);
        if (personnel == null)
        {
            throw new KeyNotFoundException($"Personel bulunamadı (ID: {personnelId})");
        }

        // Admin reset - no old password verification needed
        personnel.PasswordHash = BCrypt.Net.BCrypt.HashPassword(resetPasswordDto.NewPassword);
        personnel.UpdatedAt = DateTime.UtcNow;

        await _personnelRepository.UpdateAsync(personnel);
    }

    public async Task<CustomerPersonnel?> AuthenticateAsync(string username, string password)
    {
        var personnel = await _personnelRepository.GetByUsernameAsync(username);

        if (personnel == null)
            return null;

        if (!personnel.IsActive)
            return null;

        if (!BCrypt.Net.BCrypt.Verify(password, personnel.PasswordHash))
            return null;

        return personnel;
    }

    private async Task<CustomerPersonnelDto> MapToDtoAsync(CustomerPersonnel personnel)
    {
        // Sadece junction table'dan organizasyon bilgilerini al
        var orgAssignments = personnel.OrganizationAssignments?
            .Where(oa => !oa.IsDeleted)
            .ToList() ?? new List<CustomerPersonnelOrganization>();

        var orgIds = orgAssignments.Select(oa => oa.CustomerOrganizationId).ToList();

        // İlk organizasyon atamasından bilgileri al (birincil organizasyon)
        var primaryAssignment = orgAssignments.FirstOrDefault();

        return new CustomerPersonnelDto
        {
            Id = personnel.Id,
            CustomerId = personnel.CustomerId,
            CustomerName = personnel.Customer?.CompanyName ?? "",
            OrganizationId = primaryAssignment?.CustomerOrganizationId,
            OrganizationName = primaryAssignment?.CustomerOrganization?.Name,
            SupervisorId = primaryAssignment?.SupervisorId,
            SupervisorName = primaryAssignment?.Supervisor != null
                ? $"{primaryAssignment.Supervisor.FirstName} {primaryAssignment.Supervisor.LastName}"
                : null,
            Username = personnel.Username,
            Email = personnel.Email,
            FirstName = personnel.FirstName,
            LastName = personnel.LastName,
            FullName = personnel.FullName,
            PhoneNumber = personnel.PhoneNumber,
            Department = personnel.Department,
            Title = personnel.Title,
            Role = CustomerPersonnelRoles.GetById(personnel.RoleId)?.SystemName ?? "",
            RoleName = await GetRoleNameAsync(personnel.RoleId),
            IsActive = personnel.IsActive,
            Notes = personnel.Notes,
            TaskAssignmentCount = personnel.TaskAssignments?.Count ?? 0,
            CreatedAt = personnel.CreatedAt,
            UpdatedAt = personnel.UpdatedAt,
            OrganizationAssignmentCount = orgIds.Count,
            OrganizationIds = orgIds
        };
    }

    // Uniqueness checks
    public async Task<bool> ExistsByUsernameAsync(string username, int? excludeId = null)
    {
        return await _personnelRepository.ExistsByUsernameAsync(username, excludeId);
    }

    public async Task<bool> ExistsByEmailAsync(string email, int? excludeId = null)
    {
        return await _personnelRepository.ExistsByEmailAsync(email, excludeId);
    }

    // Firma bazlı email kontrolü
    private async Task<bool> ExistsByEmailInCompanyAsync(string email, int customerId, int? excludeId = null)
    {
        return await _personnelRepository.ExistsByEmailInCompanyAsync(email, customerId, excludeId);
    }
}
