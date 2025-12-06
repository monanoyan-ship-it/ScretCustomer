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

    public CustomerPersonnelService(
        ICustomerPersonnelRepository personnelRepository,
        ICustomerRepository customerRepository)
    {
        _personnelRepository = personnelRepository;
        _customerRepository = customerRepository;
    }

    public async Task<CustomerPersonnelDto?> GetByIdAsync(Guid id)
    {
        var personnel = await _personnelRepository.GetByIdAsync(id, includeDetails: true);
        return personnel == null ? null : MapToDto(personnel);
    }

    public async Task<IEnumerable<CustomerPersonnelDto>> GetAllAsync(bool includeInactive = false)
    {
        var personnel = await _personnelRepository.GetAllAsync(includeInactive);
        return personnel.Select(MapToDto);
    }

    public async Task<IEnumerable<CustomerPersonnelDto>> GetByCustomerIdAsync(Guid customerId, bool includeInactive = false)
    {
        var personnel = await _personnelRepository.GetByCustomerIdAsync(customerId, includeInactive);
        return personnel.Select(MapToDto);
    }

    public async Task<CustomerPersonnelDto?> GetByUsernameAsync(string username)
    {
        var personnel = await _personnelRepository.GetByUsernameAsync(username);
        return personnel == null ? null : MapToDto(personnel);
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

        // Check if email already exists
        if (await _personnelRepository.ExistsByEmailAsync(createDto.Email))
        {
            throw new InvalidOperationException("Bu e-posta adresi ile kayıtlı bir personel zaten mevcut");
        }

        var personnel = new CustomerPersonnel
        {
            CustomerId = createDto.CustomerId,
            Username = createDto.Username,
            Email = createDto.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(createDto.Password),
            FirstName = createDto.FirstName,
            LastName = createDto.LastName,
            PhoneNumber = createDto.PhoneNumber,
            Department = createDto.Department,
            Title = createDto.Title,
            Role = createDto.Role,
            IsActive = createDto.IsActive,
            Notes = createDto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        var createdPersonnel = await _personnelRepository.CreateAsync(personnel);

        // Reload with details
        var result = await _personnelRepository.GetByIdAsync(createdPersonnel.Id, includeDetails: true);
        return MapToDto(result!);
    }

    public async Task<CustomerPersonnelDto> UpdateAsync(Guid id, UpdateCustomerPersonnelDto updateDto)
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
        personnel.Role = updateDto.Role;
        personnel.IsActive = updateDto.IsActive;
        personnel.Notes = updateDto.Notes;

        var updatedPersonnel = await _personnelRepository.UpdateAsync(personnel);

        // Reload with details
        var result = await _personnelRepository.GetByIdAsync(updatedPersonnel.Id, includeDetails: true);
        return MapToDto(result!);
    }

    public async Task DeleteAsync(Guid id)
    {
        var personnel = await _personnelRepository.GetByIdAsync(id);
        if (personnel == null)
        {
            throw new KeyNotFoundException($"Personel bulunamadı (ID: {id})");
        }

        await _personnelRepository.DeleteAsync(id);
    }

    private static CustomerPersonnelDto MapToDto(CustomerPersonnel personnel)
    {
        return new CustomerPersonnelDto
        {
            Id = personnel.Id,
            CustomerId = personnel.CustomerId,
            CustomerName = personnel.Customer?.CompanyName ?? "",
            Username = personnel.Username,
            Email = personnel.Email,
            FirstName = personnel.FirstName,
            LastName = personnel.LastName,
            FullName = personnel.FullName,
            PhoneNumber = personnel.PhoneNumber,
            Department = personnel.Department,
            Title = personnel.Title,
            Role = personnel.Role,
            RoleName = GetRoleName(personnel.Role),
            IsActive = personnel.IsActive,
            Notes = personnel.Notes,
            TaskAssignmentCount = personnel.TaskAssignments?.Count ?? 0,
            CreatedAt = personnel.CreatedAt,
            UpdatedAt = personnel.UpdatedAt
        };
    }

    private static string GetRoleName(CustomerPersonnelRole role)
    {
        return role switch
        {
            CustomerPersonnelRole.CustomerManager => "Müşteri Yöneticisi",
            CustomerPersonnelRole.CustomerSupervisor => "Müşteri Süpervizörü",
            CustomerPersonnelRole.CustomerOperator => "Müşteri Operatörü",
            CustomerPersonnelRole.CustomerViewer => "Müşteri Görüntüleyici",
            _ => role.ToString()
        };
    }
}
